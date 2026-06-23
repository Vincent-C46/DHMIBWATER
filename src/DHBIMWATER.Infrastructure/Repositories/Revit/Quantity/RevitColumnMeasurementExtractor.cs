using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using DHBIMWATER.Application.Interfaces.Geometry;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Infrastructure.Helpers;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Quantity
{
    public class RevitColumnMeasurementExtractor : IElementMeasurementExtractor
    {
        private readonly Func<Document?> _doc;
        private readonly IIntersectingElementFinder _finder;
        private readonly IFaceClassifier _classifier;

        public RevitColumnMeasurementExtractor(Func<Document?> doc, IIntersectingElementFinder finder, IFaceClassifier classifier)
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
                .OfClass(typeof(FamilyInstance))
                .OfCategory(BuiltInCategory.OST_StructuralColumns)
                .WhereElementIsNotElementType()
                .Select(r => r.Id.Value);
        }

        public ElementMeasurements Extract(long elementId)
        {
            var doc = _doc();
            var empty = new ElementMeasurements { ElementId = elementId };
            if (doc == null) return empty;

            var column = doc.GetElement(new ElementId(elementId)) as FamilyInstance;
            if (column == null) return empty;

            var refFaceDict = _classifier.GetFaceAreas(elementId);
            var deductionByFaceType = QuantityExtractorHelper.GroupDeductions(_finder.FindContactAreas(elementId));

            // ── 길이 / 방향 ────────────────────────────────────────────────
            var length = UC.FtToM(column.get_Parameter(BuiltInParameter.INSTANCE_LENGTH_PARAM)?.AsDouble() ?? 0);

            var dc = column.GetSweptProfile().GetDrivingCurve();
            var columnDirection = (dc.GetEndPoint(1) - dc.GetEndPoint(0)).Normalize();

            var splitSolids = RevitGeometryHelper.GetSolids(column)
                .SelectMany(s => { try { return SolidUtils.SplitVolumes(s); } catch { return [s]; } })
                .Where(s => s.Volume > 1e-9)
                .ToList();

            double effectiveLength = length;
            double totalLength = splitSolids
                .Select(solid => solid.Edges.Cast<Edge>()
                    .Select(e => e.AsCurve()).OfType<Line>()
                    .Where(l => Math.Abs(((l.GetEndPoint(1) - l.GetEndPoint(0)).Normalize()).DotProduct(columnDirection)) > 0.99)
                    .Select(l => l.Length)
                    .DefaultIfEmpty(0).Max())
                .Sum();
            if (totalLength > 0) effectiveLength = UC.FtToM(totalLength);

            // ── 단면적 ────────────────────────────────────────────────────
            double actualCrossSection = 0;
            var firstSolid = splitSolids.FirstOrDefault();
            if (firstSolid != null)
                actualCrossSection = UC.Ft2ToM2(RevitGeometryHelper.GetMidSectionArea(firstSolid, columnDirection));

            double volumeM3 = UC.Ft3ToM3(RevitGeometryHelper.GetSolids(column).Sum(s => s.Volume));

            // ── 단면 치수 파라미터 ────────────────────────────────────────
            var b = UC.FtToM(FamilyInstanceHelper.FindParameter(column, "b") ?? 0);
            var d = UC.FtToM(FamilyInstanceHelper.FindParameter(column, "d") ??
                             FamilyInstanceHelper.FindParameter(column, "h") ??
                             FamilyInstanceHelper.FindParameter(column, "b") ??
                             b);
            var r = UC.FtToM(FamilyInstanceHelper.FindParameter(column, "r") ??
                             FamilyInstanceHelper.FindParameter(column, "d") / 2 ??
                             FamilyInstanceHelper.FindParameter(column, "b") / 2 ??
                             0);

            // ── 원형 여부 ─────────────────────────────────────────────────
            string typeName   = column.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).AsValueString() ?? string.Empty;
            string familyName = column.Symbol.FamilyName;
            bool isCircular   = typeName.Contains("원형",    StringComparison.OrdinalIgnoreCase) ||
                                typeName.Contains("circular", StringComparison.OrdinalIgnoreCase) ||
                                familyName.Contains("원형",    StringComparison.OrdinalIgnoreCase) ||
                                familyName.Contains("circular", StringComparison.OrdinalIgnoreCase);

            // ── 5% 공차 판별 ──────────────────────────────────────────────
            const double tolerance = 0.05;
            bool rectMatches     = !isCircular && b > 0 && d > 0 && actualCrossSection > 0
                                   && Math.Abs(b * d - actualCrossSection) / actualCrossSection < tolerance;
            bool circularMatches = isCircular && r > 0 && actualCrossSection > 0
                                   && Math.Abs(Math.PI * r * r - actualCrossSection) / actualCrossSection < tolerance;

            // ── 재료 정보 ─────────────────────────────────────────────────
            var materialId  = FamilyInstanceHelper.GetMaterialId(column);
            var materialName = (materialId != null && materialId != ElementId.InvalidElementId)
                ? (doc.GetElement(materialId) as Material)?.Name ?? string.Empty
                : string.Empty;

            var materialClass    = FamilyInstanceHelper.GetStructuralAssetClass(column);
            var materialClassStr = materialClass switch
            {
                StructuralAssetClass.Metal   => "강재",
                StructuralAssetClass.Generic => "기타",
                _                            => "콘크리트"
            };
            var concWorkType = materialClassStr == "콘크리트" ? "철근콘크리트" : string.Empty;

            return new ElementMeasurements
            {
                ElementId  = elementId,
                Category   = column.Category.Name ?? string.Empty,
                CategoryId = (int)BuiltInCategory.OST_StructuralColumns,
                Values = new Dictionary<string, double>
                {
                    ["Vol"]          = volumeM3,
                    ["L"]            = effectiveLength,
                    ["B"]            = b,
                    ["D"]            = d,
                    ["R"]            = r,
                    ["A_cs"]         = actualCrossSection,
                    ["A_side_gross"] = refFaceDict.GetValueOrDefault(FaceType.Side, 0),
                    ["A_side_net"]   = QuantityExtractorHelper.GetNetArea(refFaceDict, deductionByFaceType, FaceType.Side),
                },
                Parameters = new Dictionary<string, string>
                {
                    ["MaterialClass"]  = materialClassStr,
                    ["ConcWorkType"]   = concWorkType,
                    ["MaterialName"]   = materialName,
                    ["DH_ElementCode"] = column.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                    ["UseRectFormula"] = rectMatches     ? "true" : "false",
                    ["IsCircular"]     = circularMatches ? "true" : "false",
                }
            };
        }
    }
}
