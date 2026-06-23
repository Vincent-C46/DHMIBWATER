using Autodesk.Revit.DB;
using DHBIMWATER.Application.Interfaces.Geometry;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Infrastructure.Helpers;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Quantity
{
    public class RevitFloorMeasurementExtractor : IElementMeasurementExtractor
    {
        private readonly Func<Document?> _doc;
        private readonly IIntersectingElementFinder _finder;
        private readonly IFaceClassifier _classifier;

        public RevitFloorMeasurementExtractor(Func<Document?> doc, IIntersectingElementFinder finder, IFaceClassifier classifier)
        {
            _doc = doc;
            _finder = finder;
            _classifier = classifier;
        }

        public bool CanExtract(long elementId)
        {
            var doc = _doc();
            if (doc == null) return false;
            return doc.GetElement(new ElementId(elementId)) is Floor;
        }

        public IEnumerable<long> CollectElementIds()
        {
            var doc = _doc();
            if (doc == null) return Enumerable.Empty<long>();

            return new FilteredElementCollector(doc)
                .OfClass(typeof(Floor))
                .WhereElementIsNotElementType()
                .Select(r => r.Id.Value);
        }

        public ElementMeasurements Extract(long elementId)
        {
            var doc = _doc();
            var empty = new ElementMeasurements { ElementId = elementId };
            if (doc == null) return empty;

            var floor = doc.GetElement(new ElementId(elementId)) as Floor;
            if (floor == null) return empty;

            var refFaceDict = _classifier.GetFaceAreas(elementId);
            var deductionByFaceType = QuantityExtractorHelper.GroupDeductions(_finder.FindContactAreas(elementId));

            // ── 복합구조 레이어 ─────────────────────────────────────────────
            var cs = floor.FloorType.GetCompoundStructure();
            var area = UC.Ft2ToM2(floor.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED).AsDouble());
            double thickness = UC.FtToM(cs.GetLayers()
                .Where(l => l.Function == MaterialFunctionAssignment.Structure)
                .Sum(l => l.Width));

            var structureLayer = cs.GetLayers().FirstOrDefault(l => l.Function == MaterialFunctionAssignment.Structure);
            var material = doc.GetElement(structureLayer?.MaterialId) as Material;
            var materialName = material?.Name ?? string.Empty;

            double volume = UC.Ft3ToM3(RevitGeometryHelper.GetSolids(floor).Sum(s => s.Volume));

            // ── 재료 클래스 (복합구조 레이어 StructuralAsset) ─────────────
            StructuralAssetClass? materialClass = null;
            if (material != null)
            {
                var assetId = material.StructuralAssetId;
                if (assetId != null && assetId != ElementId.InvalidElementId)
                    materialClass = (doc.GetElement(assetId) as PropertySetElement)?.GetStructuralAsset()?.StructuralAssetClass;
            }

            var materialClassStr = materialClass switch
            {
                StructuralAssetClass.Metal   => "강재",
                StructuralAssetClass.Generic => "기타",
                _                            => "콘크리트"
            };

            string concWorkType = materialClassStr == "콘크리트"
                ? (thickness < 0.15 || materialName.Contains("무근") ? "무근콘크리트" : "철근콘크리트")
                : string.Empty;

            return new ElementMeasurements
            {
                ElementId  = elementId,
                Category   = floor.Category.Name ?? string.Empty,
                CategoryId = (int)BuiltInCategory.OST_Floors,
                Values = new Dictionary<string, double>
                {
                    ["A"]              = area,
                    ["Thk"]            = thickness,
                    ["Vol"]            = volume,
                    ["A_bottom_gross"] = refFaceDict.GetValueOrDefault(FaceType.Bottom, 0),
                    ["A_side_gross"]   = refFaceDict.GetValueOrDefault(FaceType.Side,   0),
                    ["A_bottom_net"]   = QuantityExtractorHelper.GetNetArea(refFaceDict, deductionByFaceType, FaceType.Bottom),
                    ["A_side_net"]     = QuantityExtractorHelper.GetNetArea(refFaceDict, deductionByFaceType, FaceType.Side),
                },
                Parameters = new Dictionary<string, string>
                {
                    ["MaterialClass"]  = materialClassStr,
                    ["ConcWorkType"]   = concWorkType,
                    ["MaterialName"]   = materialName,
                    ["DH_ElementCode"] = floor.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                }
            };
        }
    }
}
