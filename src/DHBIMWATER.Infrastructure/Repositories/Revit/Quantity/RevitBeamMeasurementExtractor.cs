using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using DHBIMWATER.Application.Interfaces.Geometry;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Infrastructure.Helpers;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Quantity
{
    public class RevitBeamMeasurementExtractor : IElementMeasurementExtractor
    {
        private readonly Func<Document?> _doc;
        private readonly IIntersectingElementFinder _finder;
        private readonly IFaceClassifier _classifier;

        public RevitBeamMeasurementExtractor(Func<Document?> doc, IIntersectingElementFinder finder, IFaceClassifier classifier)
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
                .OfClass(typeof(FamilyInstance))
                .OfCategory(BuiltInCategory.OST_StructuralFraming)
                .WhereElementIsNotElementType()
                .Select(r => r.Id.Value);
        }

        public ElementMeasurements Extract(long elementId)
        {
            var doc = _doc();
            var empty = new ElementMeasurements { ElementId = elementId };
            if (doc == null) return empty;

            var beam = doc.GetElement(new ElementId(elementId)) as FamilyInstance;
            if (beam == null) return empty;

            var refFaceDict = _classifier.GetFaceAreas(elementId);
            var deductionByFaceType = QuantityExtractorHelper.GroupDeductions(_finder.FindContactAreas(elementId));

            // ── 길이 / 방향 ────────────────────────────────────────────────
            var length = UC.FtToM(beam.get_Parameter(BuiltInParameter.INSTANCE_LENGTH_PARAM)?.AsDouble() ?? 0);

            var lc = beam.Location as LocationCurve;
            var beamDirection = (lc.Curve.GetEndPoint(1) - lc.Curve.GetEndPoint(0)).Normalize();

            var splitSolids = RevitGeometryHelper.GetSolids(beam)
                .SelectMany(s => { try { return SolidUtils.SplitVolumes(s); } catch { return [s]; } })
                .Where(s => s.Volume > 1e-9)
                .ToList();

            double effectiveLength = length;
            double totalLength = splitSolids
                .Select(solid => solid.Edges.Cast<Edge>()
                    .Select(e => e.AsCurve()).OfType<Line>()
                    .Where(l => Math.Abs(((l.GetEndPoint(1) - l.GetEndPoint(0)).Normalize()).DotProduct(beamDirection)) > 0.99)
                    .Select(l => l.Length)
                    .DefaultIfEmpty(0).Max())
                .Sum();
            if (totalLength > 0) effectiveLength = UC.FtToM(totalLength);

            // ── 단면적 ────────────────────────────────────────────────────
            double actualCrossSection = 0;
            var firstSolid = splitSolids.FirstOrDefault();
            if (firstSolid != null)
                actualCrossSection = UC.Ft2ToM2(RevitGeometryHelper.GetMidSectionArea(firstSolid, beamDirection));

            double volumeM3 = UC.Ft3ToM3(RevitGeometryHelper.GetSolids(beam).Sum(s => s.Volume));

            // ── 단면 치수 파라미터 (b=폭, d/h=춤) ────────────────────────
            var b = UC.FtToM(FamilyInstanceHelper.FindParameter(beam, "b") ??
                             FamilyInstanceHelper.FindParameter(beam, "width") ??
                             FamilyInstanceHelper.FindParameter(beam, "폭") ?? 0);
            var d = UC.FtToM(FamilyInstanceHelper.FindParameter(beam, "h") ??
                             FamilyInstanceHelper.FindParameter(beam, "d") ??
                             FamilyInstanceHelper.FindParameter(beam, "높이") ??
                             FamilyInstanceHelper.FindParameter(beam, "Height") ?? 0);

            // ── 5% 공차 판별 (보는 원형 없음) ────────────────────────────
            const double tolerance = 0.05;
            bool rectMatches = b > 0 && d > 0 && actualCrossSection > 0
                               && Math.Abs(b * d - actualCrossSection) / actualCrossSection < tolerance;

            // ── 재료 정보 ─────────────────────────────────────────────────
            var materialId   = FamilyInstanceHelper.GetMaterialId(beam);
            var materialName = (materialId != null && materialId != ElementId.InvalidElementId)
                ? (doc.GetElement(materialId) as Material)?.Name ?? string.Empty
                : string.Empty;

            var materialClass    = FamilyInstanceHelper.GetStructuralAssetClass(beam);
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
                Category   = beam.Category.Name ?? string.Empty,
                CategoryId = (int)BuiltInCategory.OST_StructuralFraming,
                Values = new Dictionary<string, double>
                {
                    ["Vol"]            = volumeM3,
                    ["L"]              = effectiveLength,
                    ["B"]              = b,
                    ["D"]              = d,
                    ["A_cs"]           = actualCrossSection,
                    ["A_bottom_gross"] = refFaceDict.GetValueOrDefault(FaceType.Bottom, 0),
                    ["A_left_gross"]   = refFaceDict.GetValueOrDefault(FaceType.Left,   0),
                    ["A_right_gross"]  = refFaceDict.GetValueOrDefault(FaceType.Right,  0),
                    ["A_end_gross"]    = refFaceDict.GetValueOrDefault(FaceType.End,    0),
                    ["A_bottom_net"]   = QuantityExtractorHelper.GetNetArea(refFaceDict, deductionByFaceType, FaceType.Bottom),
                    ["A_left_net"]     = QuantityExtractorHelper.GetNetArea(refFaceDict, deductionByFaceType, FaceType.Left),
                    ["A_right_net"]    = QuantityExtractorHelper.GetNetArea(refFaceDict, deductionByFaceType, FaceType.Right),
                    ["A_end_net"]      = QuantityExtractorHelper.GetNetArea(refFaceDict, deductionByFaceType, FaceType.End),
                },
                Parameters = new Dictionary<string, string>
                {
                    ["MaterialClass"]  = materialClassStr,
                    ["ConcWorkType"]   = concWorkType,
                    ["MaterialName"]   = materialName,
                    ["DH_ElementCode"] = beam.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                    ["UseRectFormula"] = rectMatches ? "true" : "false",
                    ["IsCircular"]     = "false",
                }
            };
        }
    }
}
