using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using DHBIMWATER.Application.Interfaces.Geometry;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Core.Structures;
using DHBIMWATER.Infrastructure.Helpers;
using DocumentFormat.OpenXml.Drawing.Charts;
using System.Diagnostics;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Quantity
{
    public class RevitColumnExtractor : IQuantityExtractor
    {
        private readonly Func<Document?> _doc;
        private readonly IIntersectingElementFinder _finder;
        private readonly IFaceClassifier _classifier;
        public RevitColumnExtractor(Func<Document?> doc, IIntersectingElementFinder finder, IFaceClassifier classifier)
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

            return elem is FamilyInstance fi && fi.Category.Id.Value == (int)BuiltInCategory.OST_StructuralColumns;
        }
        public IEnumerable<long> CollectElementIds()
        {
            var doc = _doc();
            if (doc == null) return Enumerable.Empty<long>();

            return new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_StructuralColumns)
            .WhereElementIsNotElementType()
            .Select(r => r.Id.Value);
        }

        public IEnumerable<QuantityItem> Extract(long elementId)
        {
            var doc = _doc();
            if (doc == null) return Enumerable.Empty<QuantityItem>();

            var column = doc.GetElement(new ElementId(elementId)) as FamilyInstance;
            if (column == null) return Enumerable.Empty<QuantityItem>();

            var quantityItems = new List<QuantityItem>();

            // 객체 추출값
            var length = UC.FtToM(column.get_Parameter(BuiltInParameter.INSTANCE_LENGTH_PARAM)?.AsDouble() ?? 0);
            double effectiveLength = length;    // 초기값
            double totalLength = 0;
            var parallelEdgeLengths = new List<double>();
            var allEndFaceAreas = new List<double>();

            // Split Solid 순회
            var splitSolids = RevitGeometryHelper.GetSolids(column).SelectMany(SolidUtils.SplitVolumes).ToList();
            var solidCrossSectionAreas = new List<double>(); // SplitSolid별 단면적 저장

            var dc = column.GetSweptProfile().GetDrivingCurve();
            var columnDirection = (dc.GetEndPoint(1) - dc.GetEndPoint(0)).Normalize();

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
                        var dotProduct = Math.Abs(edgeDir.DotProduct(columnDirection));

                        if (dotProduct > 0.99)
                        {
                            parallelEdgeCnt++;
                            maxEdgeLength = Math.Max(maxEdgeLength, line.Length);
                        }
                    }
                }
                parallelEdgeLengths.Add(maxEdgeLength);

                // Split Solid별 단면적 추출 (기둥 Dir과 동일한 방향 Face)
                foreach (Face face in splitSolid.Faces)
                {
                    var faceNormal = face.ComputeNormal(new UV(0.5, 0.5));
                    var dotProduct = Math.Abs(columnDirection.DotProduct(faceNormal));

                    // 기둥 방향과 평행한 면 (End면)
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

                    //if (maxDiff / avgAreaInSolid > 0.05)
                    //{
                    //    Debug.WriteLine($"⚠️ 변단면: 차이 {maxDiff / avgAreaInSolid * 100:F1}%");
                    //}
                }
            }
            totalLength = parallelEdgeLengths.Sum();
            if (totalLength > 0) effectiveLength = UC.FtToM(totalLength);

            double avgArea = solidCrossSectionAreas.Any() ? solidCrossSectionAreas.Average() : 0;

            var refFaceDict = _classifier.GetFaceAreas(elementId);
            var deductionByFaceType = QuantityExtractorHelper.GroupDeductions(_finder.FindContactAreas(elementId));

            string materialName = string.Empty;
            var materialId = column.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM)?.AsElementId()
                                ?? column.Document.GetElement(column.GetTypeId()).get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM)?.AsElementId();

            if (materialId == null || materialId == ElementId.InvalidElementId)
                materialName = string.Empty;
            else
                materialName = (doc.GetElement(materialId) as Material).Name;


            // 객체 추출값
            var b = UC.FtToM(FamilyInstanceHelper.FindParameter(column, "b") ?? 0);
            var d = UC.FtToM(FamilyInstanceHelper.FindParameter(column, "d") ??
                             FamilyInstanceHelper.FindParameter(column, "h") ??
                             b);
            var r = UC.FtToM(FamilyInstanceHelper.FindParameter(column, "r") ??
                             FamilyInstanceHelper.FindParameter(column, "d") / 2  ?? 
                             FamilyInstanceHelper.FindParameter(column, "b") / 2 ??
                             0);

            string typeName = column.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).AsValueString() ?? string.Empty;
            string familyName = column.Symbol.FamilyName;
            bool isCircular = typeName.Contains("원형", StringComparison.OrdinalIgnoreCase) || 
                              typeName.Contains("circular", StringComparison.OrdinalIgnoreCase) ||
                              familyName.Contains("원형", StringComparison.OrdinalIgnoreCase) ||
                              familyName.Contains("circular", StringComparison.OrdinalIgnoreCase);

            // 추출된 매개변수 유효성에 따라 공식 분기
            string concFormula;
            Dictionary<string, double> varDict;

            if (isCircular && (r > 0))
            {
                concFormula = "PI x R^2 x L";
                varDict = new Dictionary<string, double>
                {
                    ["R"] = r,
                    ["L"] = effectiveLength,
                };
            }
            else if (!isCircular && b > 0 && d > 0)
            {
                concFormula = "B x D x L";
                varDict = new Dictionary<string, double>
                {
                    ["B"] = b,
                    ["D"] = d,
                    ["L"] = effectiveLength,
                };
            }
            else
            {
                concFormula = "A x L";
                varDict = new Dictionary<string, double>
                {
                    ["A"] = UC.Ft2ToM2(avgArea),
                    ["L"] = effectiveLength,
                };
            }

            string? concRendered = FormulaCalculator.Render(concFormula, varDict);
            double concValue = UC.Ft3ToM3(RevitGeometryHelper.GetSolids(column).Sum(s => s.Volume));


            // 철근콘크리트
            var concreteItem = new QuantityItem
            {
                ElementId = elementId,
                Category = column.Category.Name ?? string.Empty,
                ElementCode = column.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                WorkType = "철근콘크리트",
                Specification = materialName,
                RawFormula = concFormula,
                RenderedFormula = concRendered ?? string.Empty,
                Value = concValue,
                Unit = "m³"
            };
            var listToAdd = new List<QuantityItem>() { concreteItem, };
            quantityItems.AddRange(listToAdd);

            return quantityItems;
        }
    }
}
