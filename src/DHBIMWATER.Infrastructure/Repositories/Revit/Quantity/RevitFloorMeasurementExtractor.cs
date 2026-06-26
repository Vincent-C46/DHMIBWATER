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

        private const double ShoringHeightThresholdM = 4.2; // 4.2m 기준으로 동바리 종류를 구분 (강관동바리 / 시스템 동바리)
        private const double ShoringCoefficient      = 0.9; // 동바리 계수 (동바리 면적 (or 부피) * 0.9)

        private static string CalcShoringRange(double h)
        {
            if (h <= 0)                          return string.Empty;
            if (h <= ShoringHeightThresholdM)
            {
                if (h <= 3.5) return "강관_3.5";
                return "강관_4.2";
            }
            if (h <= 5)  return "시스템_5";
            if (h <= 10) return "시스템_10";
            if (h <= 20) return "시스템_20";
            return "시스템_30";
        }

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

            // ── 동바리 ────────────────────────────────────────────────────────
            double aBottomGross   = refFaceDict.GetValueOrDefault(FaceType.Bottom, 0);
            double shoringHeightM = CalcShoringHeight(floor, doc);
            string shoringRange   = CalcShoringRange(shoringHeightM);

            return new ElementMeasurements
            {
                ElementId  = elementId,
                Category   = floor.Category.Name ?? string.Empty,
                CategoryId = (int)BuiltInCategory.OST_Floors,
                Values = new Dictionary<string, double>
                {
                    ["A"]                    = area,
                    ["Thk"]                  = thickness,
                    ["Vol"]                  = volume,
                    ["A_bottom_gross"]       = aBottomGross,
                    ["A_side_gross"]         = refFaceDict.GetValueOrDefault(FaceType.Side,        0),
                    ["A_opening_side_gross"] = refFaceDict.GetValueOrDefault(FaceType.OpeningSide, 0),
                    ["A_bottom_net"]         = QuantityExtractorHelper.GetNetArea(refFaceDict, deductionByFaceType, FaceType.Bottom),
                    ["A_side_net"]           = QuantityExtractorHelper.GetNetArea(refFaceDict, deductionByFaceType, FaceType.Side),
                    ["A_opening_side_net"]   = QuantityExtractorHelper.GetNetArea(refFaceDict, deductionByFaceType, FaceType.OpeningSide),
                    ["H_shoring"]            = shoringHeightM,
                    ["ShoringFactor"]        = ShoringCoefficient,
                },
                Parameters = new Dictionary<string, string>
                {
                    ["MaterialClass"]    = materialClassStr,
                    ["ConcWorkType"]     = concWorkType,
                    ["MaterialName"]     = materialName,
                    ["DH_ElementCode"]   = floor.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                    ["ShoringRange"] = shoringRange,
                }
            };
        }

        private static double CalcShoringHeight(Floor floor, Document doc)
        {
            var bbox = floor.get_BoundingBox(null);
            if (bbox == null) return 0;

            double thisBottomZ = bbox.Min.Z;

            // 바로 아래 슬래브: BBox 상단이 현재 슬래브 하면보다 낮은 것 중 가장 가까운 것
            var lowerBBoxMaxZ = new FilteredElementCollector(doc)
                .OfClass(typeof(Floor))
                .WhereElementIsNotElementType()
                .Where(e => e.Id != floor.Id)
                .Select(e => e.get_BoundingBox(null))
                .Where(b => b != null && b.Max.Z < thisBottomZ)
                .OrderByDescending(b => b.Max.Z)
                .FirstOrDefault()?.Max.Z;

            if (lowerBBoxMaxZ == null) return 0;

            return UC.FtToM(thisBottomZ - lowerBBoxMaxZ.Value);
        }
    }
}
