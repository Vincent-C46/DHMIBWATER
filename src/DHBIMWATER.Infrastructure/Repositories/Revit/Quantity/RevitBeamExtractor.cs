using Autodesk.Revit.DB;
using DHBIMWATER.Application.Interfaces.Geometry;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Infrastructure.Helpers;
using DHBIMWATER.Infrastructure.Repositories.Revit.Geometry;
using System.Diagnostics;
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
            if (beam == null)
                return Enumerable.Empty<QuantityItem>();

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

            // 실제 유효 길이 계산 (Solid Edge 기반)
            double effectiveLength = length;
            
            var lc = beam.Location as LocationCurve;
            var beamDirection = (lc.Curve.GetEndPoint(1) - lc.Curve.GetEndPoint(0)).Normalize();
            var solids = RevitGeometryHelper.GetSolids(beam).ToList();

            if (solids.Any())
            {
                double totalLength = 0;

                foreach (var solid in solids)
                {
                    var parallelEdgeLengths = new List<double>();
                    // Solid의 모든 Edge 검사
                    foreach (Edge edge in solid.Edges)
                    {
                        var curve = edge.AsCurve();
                        if (curve is Line line)
                        {
                            var edgeDirection = (line.GetEndPoint(1) - line.GetEndPoint(0)).Normalize();
                            var dotProduct = Math.Abs(edgeDirection.DotProduct(beamDirection));

                            // 보 방향과 평행한 Edge 찾기 (내적이 1에 가까운)
                            if (dotProduct > 0.99)
                            {
                                parallelEdgeLengths.Add(line.Length);
                                Debug.WriteLine($"  평행 Edge 발견: {UC.FtToM(line.Length):F3}m (dotProduct={dotProduct:F3})");
                            }
                        }
                    }

                    // 서로 다른 길이만 추출 (중복 제거)
                    var distinctLengths = parallelEdgeLengths
                        .Select(l => Math.Round(l, 6)) // 부동소수점 오차 처리
                        .Distinct()
                        .ToList();

                    var sumLength = distinctLengths.Sum();
                    
                    Debug.WriteLine($"Solid Volume={UC.Ft3ToM3(solid.Volume):F3}m³, 평행Edge수={parallelEdgeLengths.Count}, 고유길이수={distinctLengths.Count}, 합계={UC.FtToM(sumLength):F3}m");
                    totalLength += sumLength;
                }

                Debug.WriteLine($"총 유효 길이: {UC.FtToM(totalLength):F3}m");

                if (totalLength > 0)
                {
                    effectiveLength = UC.FtToM(totalLength);
                }
            }


            // 슬래브에 의한 유효 높이 공제 계산
            double effectiveHeight = h;
            var topDeductions = deductionByFaceType.GetValueOrDefault(FaceType.Top, new List<(FaceType, long, double)>());
            if (topDeductions.Any())
            {
                // Top면에서 슬래브를 찾아 두께 공제
                foreach (var (_, neighborId, _) in topDeductions)
                {
                    var neighbor = doc.GetElement(new ElementId(neighborId));
                    if (neighbor?.Category?.Id.Value == (int)BuiltInCategory.OST_Floors)
                    {
                        var floorThickness = neighbor.get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM)?.AsDouble() ?? 0;
                        if (floorThickness > 0)
                        {
                            effectiveHeight = Math.Max(0, h - UC.FtToM(floorThickness));
                            break; // 첫 번째 슬래브만 적용
                        }
                    }
                }
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
                ["B"] = b,
                ["H"] = effectiveHeight,
                ["L"] = effectiveLength,
            };

            // 콘크리트 체적 계산 (실제 Solid Volume 사용)
            double concValue = UC.Ft3ToM3(RevitGeometryHelper.GetSolids(beam).Sum(s => s.Volume));

            // 이론값과 실제값 비교를 위한 참고 공식
            double theoreticalVolume = b * effectiveHeight * effectiveLength;
            const string concFormula = "Solid Volume (기둥/슬래브 등에 의한 공제 포함)";
            string concRendered = $"{concValue:F3} m³ (B={b:F3} × H_eff={effectiveHeight:F3} × L_eff={effectiveLength:F3} 기준 이론값: {theoreticalVolume:F3} m³)";

            if (effectiveHeight < h)
            {
                concRendered += $" [슬래브 두께 {(h - effectiveHeight):F3}m 공제]";
            }

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

                var formworkItem = new QuantityItem
                {
                    ElementId = elementId,
                    Category = beam.Category.Name ?? string.Empty,
                    ElementCode = beam.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                    WorkType = "거푸집",
                    Specification = $"{faceType}면",
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
