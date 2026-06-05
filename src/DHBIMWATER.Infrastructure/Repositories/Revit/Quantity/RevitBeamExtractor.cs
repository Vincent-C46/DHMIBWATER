using Autodesk.Revit.DB;
using DHBIMWATER.Application.Interfaces.Geometry;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Infrastructure.Helpers;
using DHBIMWATER.Infrastructure.Repositories.Revit.Geometry;
using System.Diagnostics;
using System.DirectoryServices;
using System.Reflection;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;


namespace DHBIMWATER.Infrastructure.Repositories.Revit.Quantity
{
    public class RevitBeamExtractor : IQuantityExtractor
    {
        private readonly Func<Document?> _doc;
        private readonly IIntersectingElementFinder _finder;
        private readonly IFaceClassifier _classifier;

        public RevitBeamExtractor(Func<Document?> doc, IIntersectingElementFinder finder, IFaceClassifier classifier)
        {
            _doc = doc;
            _finder = finder;
            _classifier = classifier;
        }

        public bool CanExtract(long elementId)
        {
            var doc = _doc();
            if (doc == null) return false;

            var elem = doc.GetElement(new ElementId(elementId));

            return elem is FamilyInstance fi && fi.Category.Id.Value == (int)BuiltInCategory.OST_StructuralFraming;
        }
        public IEnumerable<long> CollectElementIds()
        {
            var doc = _doc();
            if (doc == null) return Enumerable.Empty<long>();

            return new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_StructuralFraming)
                        .WhereElementIsNotElementType()
                        .Select(r => r.Id.Value);
        }
        public IEnumerable<QuantityItem> Extract(long elementId)
        {
            var doc = _doc();
            if (doc == null)
                return Enumerable.Empty<QuantityItem>();

            var beam = doc.GetElement(new ElementId(elementId)) as FamilyInstance;
            if (beam == null) return Enumerable.Empty<QuantityItem>();

            var refFaceDict = _classifier.GetFaceAreas(elementId);
            var contatctAreaList = _finder.FindContactAreas(elementId);

            // FaceType별 공제 면적 그룹화
            var deductionByFaceType = contatctAreaList
                .GroupBy(d => d.FaceType)
                .ToDictionary(g => g.Key, g => g.ToList());

            // 순 면적 계산
            double GetNetArea(FaceType faceType)
            {
                var faceArea = refFaceDict.GetValueOrDefault(faceType, 0);
                var deductTotal = deductionByFaceType.GetValueOrDefault(faceType, new List<(FaceType, long, double)>())
                                                     .Sum(d => d.Item3);
                return Math.Max(0, faceArea - deductTotal);
            }

            // 공제 상세 문자열 생성 (Formula용)
            string GetDeductionFormula(FaceType faceType)
            {
                var gross = refFaceDict.GetValueOrDefault(faceType, 0);

                if (!deductionByFaceType.ContainsKey(faceType) || gross == 0)
                    return $"{gross:F3}";

                var deductParts = deductionByFaceType[faceType]
                    .Select(d => $"{d.Item3:F3} (Id_{d.Item2})")
                    .ToList();

                return $"{gross:F3} - " + string.Join(" - ", deductParts);
            }

            var quantityItems = new List<QuantityItem>();

            // 객체 추출값
            var length = UC.FtToM(beam.get_Parameter(BuiltInParameter.INSTANCE_LENGTH_PARAM)?.AsDouble() ?? 0);

            var b = UC.FtToM(FamilyInstanceHelper.FindParameter(beam, "b") ?? FamilyInstanceHelper.FindParameter(beam, "width") ?? FamilyInstanceHelper.FindParameter(beam, "폭") ?? 0);
            var h = UC.FtToM(FamilyInstanceHelper.FindParameter(beam, "h") ?? FamilyInstanceHelper.FindParameter(beam, "d") ?? FamilyInstanceHelper.FindParameter(beam, "높이") ?? FamilyInstanceHelper.FindParameter(beam, "Height") ?? 0);
            string typeName = beam.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).AsValueString() ?? string.Empty;

            // 실제 유효 길이 계산 (SplitSolid의 Edge 기반)
            double effectiveLength = length;
            var lc = beam.Location as LocationCurve;
            var beamDirection = (lc.Curve.GetEndPoint(1) - lc.Curve.GetEndPoint(0)).Normalize();
            var solid = RevitGeometryHelper.GetSolid(beam);
            double totalLength = 0;
            var parallelEdgeLengths = new List<double>();
            var allEndFaceAreas = new List<double>();
            double sectionArea = 0;
            double avgArea = 0;

            // Split Solid 순회
            IList<Solid> splitSolids = SolidUtils.SplitVolumes(solid);
            var solidCrossSectionAreas = new List<double>(); // SplitSolid별 단면적 저장
            
            foreach (var splitSolid in splitSolids)
            {
                double maxEdgeLength = 0;
                int parallelEdgeCnt = 0;
                var endFaceAreasInSolid = new List<double>(); // 이 SplitSolid의 End면들

                // Split Solid 길이 추출
                foreach (Edge edge in splitSolid.Edges)
                {
                    var curve = edge.AsCurve();
                    if (curve is Line line)
                    {
                        var edgeDir = (line.GetEndPoint(1) - line.GetEndPoint(0)).Normalize();
                        var dotProduct = Math.Abs(edgeDir.DotProduct(beamDirection));

                        if (dotProduct > 0.99)
                        {
                            parallelEdgeCnt++;
                            maxEdgeLength = Math.Max(maxEdgeLength, line.Length);
                        }
                    }
                }
                parallelEdgeLengths.Add(maxEdgeLength);

                // Split Solid별 단면적 추출 (beam Dir과 동일한 방향 Face)
                foreach (Face face in splitSolid.Faces)
                {
                    var faceNormal = face.ComputeNormal(new UV(0.5, 0.5));
                    var dotProduct = Math.Abs(beamDirection.DotProduct(faceNormal));

                    // 보 방향과 평행한 면 (End면)
                    if (dotProduct > 0.99)
                    {
                        endFaceAreasInSolid.Add(face.Area);
                    }
                }

                // SplitSolid 평균 단면적 계산
                if (endFaceAreasInSolid.Count >= 1)
                {
                    var avgAreaInSolid = endFaceAreasInSolid.Average();
                    solidCrossSectionAreas.Add(avgAreaInSolid);

                    var maxDiff = endFaceAreasInSolid.Max() - endFaceAreasInSolid.Min();

                    if (maxDiff / avgAreaInSolid > 0.05)
                    {
                        Debug.WriteLine($"⚠️ 변단면: 차이 {maxDiff / avgAreaInSolid * 100:F1}%");
                    }
                }
            }
            totalLength = parallelEdgeLengths.Sum();
            if (totalLength > 0) effectiveLength = UC.FtToM(totalLength);
            
            // 모든 SplitSolid 단면적의 평균
            if (solidCrossSectionAreas.Any())
            {
                avgArea = solidCrossSectionAreas.Average();
                Debug.WriteLine($"=== 최종 평균 단면적: {UC.Ft2ToM2(avgArea):F3}m² (SplitSolid {solidCrossSectionAreas.Count}개) ===");
            }

            string materialName = string.Empty;
            var materialId = beam.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM)?.AsElementId()
                                ?? beam.Document.GetElement(beam.GetTypeId())?.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM)?.AsElementId();

            if (materialId == null || materialId == ElementId.InvalidElementId)
                materialName = string.Empty;
            else
                materialName = (doc.GetElement(materialId) as Material).Name;

            var varDict = new Dictionary<string, double>
            {
                ["A"] = UC.Ft2ToM2(avgArea),
                ["L"] = effectiveLength,
            };

            // 콘크리트 체적 계산 (실제 Solid Volume 사용)
            double concValue = UC.Ft3ToM3(RevitGeometryHelper.GetSolids(beam).Sum(s => s.Volume));

            const string concFormula = "A x L";
            string concRendered = FormulaCalculator.Render(concFormula, varDict);

            // 철근콘크리트
            var concreteItem = new QuantityItem
            {
                ElementId = elementId,
                Category = beam.Category.Name ?? string.Empty,
                ElementCode = beam.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                WorkType = "철근콘크리트",
                Specification = materialName,
                RawFormula = concFormula,
                RenderedFormula = concRendered,
                Value = concValue,
                Unit = "m³"
            };

            quantityItems.Add(concreteItem);

            // 거푸집 - 각 FaceType별로 항목 생성
            var formworkFaces = new[] { FaceType.Bottom, FaceType.Left, FaceType.Right, FaceType.End };

            foreach (var faceType in formworkFaces)
            {
                var grossArea = refFaceDict.GetValueOrDefault(faceType, 0);
                if (grossArea < 0.001) continue; // 면적이 없으면 skip

                var netArea = GetNetArea(faceType);
                var formula = GetDeductionFormula(faceType);
                var spec = faceType switch
                {
                    FaceType.Bottom => "합판4회",
                    FaceType.Left => "유로폼",
                    FaceType.Right => "유로폼",
                };

                var formworkItem = new QuantityItem
                {
                    ElementId = elementId,
                    Category = beam.Category.Name ?? string.Empty,
                    ElementCode = beam.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                    WorkType = "거푸집",
                    Specification = spec,
                    RawFormula = formula,
                    RenderedFormula = formula,
                    Value = netArea,
                    Unit = "m²"
                };

                quantityItems.Add(formworkItem);
            }

            return quantityItems;
        }
    }
}
