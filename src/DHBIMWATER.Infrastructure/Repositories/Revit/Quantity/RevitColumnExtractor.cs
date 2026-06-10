using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB.Visual;
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

            var dc = column.GetSweptProfile().GetDrivingCurve();
            var columnDirection = (dc.GetEndPoint(1) - dc.GetEndPoint(0)).Normalize();

            var splitSolids = RevitGeometryHelper.GetSolids(column)
                .SelectMany(s => { try { return SolidUtils.SplitVolumes(s); } catch { return [s]; } })
                .Where(s => s.Volume > 1e-9)
                .ToList();

            // 유효 길이: 각 SplitSolid에서 기둥 방향 최장 Edge 합산
            double effectiveLength = length;
            double totalLength = splitSolids
                .Select(solid => solid.Edges.Cast<Edge>()
                    .Select(e => e.AsCurve()).OfType<Line>()
                    .Where(l => Math.Abs(((l.GetEndPoint(1) - l.GetEndPoint(0)).Normalize()).DotProduct(columnDirection)) > 0.99)
                    .Select(l => l.Length)
                    .DefaultIfEmpty(0).Max())
                .Sum();
            if (totalLength > 0) effectiveLength = UC.FtToM(totalLength);

            // 단면적: 첫 SplitSolid 중앙을 columnDirection 법선으로 자른 단면
            double actualCrossSection = 0;
            var firstSolid = splitSolids.FirstOrDefault();
            if (firstSolid != null)
                actualCrossSection = UC.Ft2ToM2(RevitGeometryHelper.GetMidSectionArea(firstSolid, columnDirection));

            var refFaceDict = _classifier.GetFaceAreas(elementId);
            var deductionByFaceType = QuantityExtractorHelper.GroupDeductions(_finder.FindContactAreas(elementId));

            string materialName = string.Empty;
            var materialId = FamilyInstanceHelper.GetMaterialId(column);

            if (materialId == null || materialId == ElementId.InvalidElementId)
                materialName = string.Empty;
            else
                materialName = (doc.GetElement(materialId) as Material).Name;

            var materialClass = FamilyInstanceHelper.GetStructuralAssetClass(column);
            var workType = materialClass switch
            {
                StructuralAssetClass.Concrete => "철근콘크리트",
                StructuralAssetClass.Metal => "강재",
                StructuralAssetClass.Generic => "기타",
                _ => "미분류"
            };

            // 객체 추출값
            var b = UC.FtToM(FamilyInstanceHelper.FindParameter(column, "b") ?? 0);
            var d = UC.FtToM(FamilyInstanceHelper.FindParameter(column, "d") ??
                             FamilyInstanceHelper.FindParameter(column, "h") ??
                             FamilyInstanceHelper.FindParameter(column, "b") ??
                             b);
            var r = UC.FtToM(FamilyInstanceHelper.FindParameter(column, "r") ??
                             FamilyInstanceHelper.FindParameter(column, "d") / 2 ??
                             FamilyInstanceHelper.FindParameter(column, "b") / 2 ??
                             0);

            string typeName = column.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).AsValueString() ?? string.Empty;
            string familyName = column.Symbol.FamilyName;
            bool isCircular = typeName.Contains("원형", StringComparison.OrdinalIgnoreCase) ||
                              typeName.Contains("circular", StringComparison.OrdinalIgnoreCase) ||
                              familyName.Contains("원형", StringComparison.OrdinalIgnoreCase) ||
                              familyName.Contains("circular", StringComparison.OrdinalIgnoreCase);

            // 매개변수 계산값이 실제 단면적 A와 5% 이내 일치할 때만 매개변수 공식 사용
            const double tolerance = 0.05;
            string concFormula;
            Dictionary<string, double> varDict;

            bool circularMatches = isCircular && r > 0 && actualCrossSection > 0
                && Math.Abs(Math.PI * r * r - actualCrossSection) / actualCrossSection < tolerance;
            bool rectMatches = !isCircular && b > 0 && d > 0 && actualCrossSection > 0
                && Math.Abs(b * d - actualCrossSection) / actualCrossSection < tolerance;

            if (circularMatches)
            {
                concFormula = "PI x R^2 x L";
                varDict = new Dictionary<string, double>
                {
                    ["R"] = r,
                    ["L"] = effectiveLength,
                };
            }
            else if (rectMatches)
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
                    ["A"] = actualCrossSection,
                    ["L"] = effectiveLength,
                };
            }

            double volumeM3 = UC.Ft3ToM3(RevitGeometryHelper.GetSolids(column).Sum(s => s.Volume));

            switch (workType)
            {
                case "철근콘크리트":
                    var concreteItem = new QuantityItem
                    {
                        ElementId = elementId,
                        Category = column.Category.Name ?? string.Empty,
                        ElementCode = column.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                        WorkType = workType,
                        Specification = materialName,
                        RawFormula = concFormula,
                        RenderedFormula = FormulaCalculator.Render(concFormula, varDict) ?? string.Empty,
                        Value = volumeM3,
                        Unit = "m³"
                    };
                    quantityItems.Add(concreteItem);

                    var formworkFaces = new[] { FaceType.Side, };
                    foreach (var faceType in formworkFaces)
                    {
                        var grossArea = refFaceDict.GetValueOrDefault(faceType, 0);
                        if (grossArea < 0.001) continue; // 면적이 없으면 skip

                        var deducts = deductionByFaceType.TryGetValue(faceType, out var dl) ? dl : null;
                        var netArea = QuantityExtractorHelper.GetNetArea(refFaceDict, deductionByFaceType, faceType);
                        var rawFormula = QuantityExtractorHelper.GetDeductionRawFormula(refFaceDict, deductionByFaceType, faceType);
                        var renderedFormula = QuantityExtractorHelper.GetDeductionRenderedFormula(refFaceDict, deductionByFaceType, faceType);

                        var spec = faceType switch
                        {
                            FaceType.Side => "합판3회",      // 추후 세팅값으로 연동
                            _ => throw new ArgumentOutOfRangeException(),
                        };

                        var formworkItem = new QuantityItem
                        {
                            ElementId = elementId,
                            Category = column.Category.Name ?? string.Empty,
                            ElementCode = column.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                            WorkType = "거푸집",
                            Specification = spec,
                            RawFormula = rawFormula,
                            RenderedFormula = renderedFormula,
                            Value = netArea,
                            Unit = "m²",
                            GrossValue = deducts != null ? grossArea : null,
                            Deductions = deducts,
                        };

                        if (formworkItem.Value > 1e-6) quantityItems.Add(formworkItem);
                    }
                    break;

                case "강재":
                    var steelFormula = concFormula + " x UW";
                    var steelVarDict = new Dictionary<string, double>(varDict) { ["UW"] = 7.850 };
                    quantityItems.Add(new QuantityItem
                    {
                        ElementId = elementId,
                        Category = column.Category.Name ?? string.Empty,
                        ElementCode = column.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                        WorkType = workType,
                        Specification = materialName,
                        RawFormula = steelFormula,
                        RenderedFormula = FormulaCalculator.Render(steelFormula, steelVarDict) ?? string.Empty,
                        Value = volumeM3 * 7.850,
                        Unit = "ton"
                    });
                    break;

                default:
                    quantityItems.Add(new QuantityItem
                    {
                        ElementId = elementId,
                        Category = column.Category.Name ?? string.Empty,
                        ElementCode = column.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                        WorkType = workType,
                        Specification = materialName,
                        RawFormula = concFormula,
                        RenderedFormula = FormulaCalculator.Render(concFormula, varDict) ?? string.Empty,
                        Value = volumeM3,
                        Unit = "m³"
                    });
                    break;
            }

            return quantityItems;
        }

    }
}
