using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Visual;
using DHBIMWATER.Application.Interfaces.Geometry;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Infrastructure.Helpers;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Quantity
{
    public class RevitWallMeasurementExtractor : IElementMeasurementExtractor
    {
        private readonly Func<Document?> _doc;
        private readonly IIntersectingElementFinder _finder;
        private readonly IFaceClassifier _classifier;

        public RevitWallMeasurementExtractor(Func<Document?> doc, IIntersectingElementFinder finder, IFaceClassifier classifier)
        {
            _doc = doc;
            _finder = finder;
            _classifier = classifier;
        }

        public bool CanExtract(long elementId)
        {
            var doc = _doc();
            if (doc == null) return false;
            return doc.GetElement(new ElementId(elementId)) is Wall;
        }

        public IEnumerable<long> CollectElementIds()
        {
            var doc = _doc();
            if (doc == null) return Enumerable.Empty<long>();

            return new FilteredElementCollector(doc)
                .OfClass(typeof(Wall))
                .WhereElementIsNotElementType()
                .Select(r => r.Id.Value);
        }

        public ElementMeasurements Extract(long elementId)
        {
            var doc = _doc();
            var empty = new ElementMeasurements { ElementId = elementId };
            if (doc == null) return empty;

            var wall = doc.GetElement(new ElementId(elementId)) as Wall;
            if (wall == null) return empty;

            var refFaceDict = _classifier.GetFaceAreas(elementId);
            var deductionByFaceType = QuantityExtractorHelper.GroupDeductions(_finder.FindContactAreas(elementId));

            var cs = wall.WallType.GetCompoundStructure();
            var area = UC.Ft2ToM2(wall.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED).AsDouble());
            double thickness = UC.FtToM(cs.GetLayers()
                .Where(l => l.Function == MaterialFunctionAssignment.Structure)
                .Sum(l => l.Width));

            var structureLayer = cs.GetLayers().FirstOrDefault(l => l.Function == MaterialFunctionAssignment.Structure);
            var material = doc.GetElement(structureLayer?.MaterialId) as Material;
            var materialName = material?.Name ?? string.Empty;

            var lc = wall.Location as LocationCurve;
            double wallLength = lc != null ? UC.FtToM(lc.Curve.Length) : 0;
            double wallHeight = UC.FtToM(wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM)?.AsDouble() ?? 0);
            double volume = UC.Ft3ToM3(RevitGeometryHelper.GetSolids(wall).Sum(s => s.Volume));

            // 벽체 재료는 복합구조 레이어에 있으므로 직접 StructuralAsset 조회
            StructuralAssetClass? materialClass = null;
            if (material != null)
            {
                var assetId = material.StructuralAssetId;
                if (assetId != null && assetId != ElementId.InvalidElementId)
                    materialClass = (doc.GetElement(assetId) as PropertySetElement)?.GetStructuralAsset()?.StructuralAssetClass;
            }

            // null(재료 미지정) → 콘크리트 가정 (기존 WallExtractor 동작과 동일)
            var materialClassStr = materialClass switch
            {
                StructuralAssetClass.Metal    => "강재",
                StructuralAssetClass.Generic  => "기타",
                _                             => "콘크리트"
            };
            var concWorkType = materialClassStr == "콘크리트" ? "철근콘크리트" : string.Empty;

            bool isExterior = wall.LookupParameter("DH_IsExterior")?.AsInteger() == 1;

            return new ElementMeasurements
            {
                ElementId  = elementId,
                Category   = wall.Category.Name ?? string.Empty,
                CategoryId = (int)BuiltInCategory.OST_Walls,
                Values = new Dictionary<string, double>
                {
                    ["Vol"]           = volume,
                    ["H"]             = wallHeight,
                    ["L"]             = wallLength,
                    ["A"]             = area,
                    ["Thk"]           = thickness,
                    ["A_left_gross"]  = refFaceDict.GetValueOrDefault(FaceType.Left,  0),
                    ["A_right_gross"] = refFaceDict.GetValueOrDefault(FaceType.Right, 0),
                    ["A_end_gross"]          = refFaceDict.GetValueOrDefault(FaceType.End,         0),
                    ["A_opening_side_gross"] = refFaceDict.GetValueOrDefault(FaceType.OpeningSide, 0),
                    ["A_left_net"]           = QuantityExtractorHelper.GetNetArea(refFaceDict, deductionByFaceType, FaceType.Left),
                    ["A_right_net"]          = QuantityExtractorHelper.GetNetArea(refFaceDict, deductionByFaceType, FaceType.Right),
                    ["A_end_net"]            = QuantityExtractorHelper.GetNetArea(refFaceDict, deductionByFaceType, FaceType.End),
                    ["A_opening_side_net"]   = QuantityExtractorHelper.GetNetArea(refFaceDict, deductionByFaceType, FaceType.OpeningSide),
                },
                Parameters = new Dictionary<string, string>
                {
                    ["MaterialClass"]  = materialClassStr,
                    ["ConcWorkType"]   = concWorkType,
                    ["MaterialName"]   = materialName,
                    ["DH_IsExterior"]  = isExterior ? "1" : "0",
                    ["DH_ElementCode"] = wall.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                }
            };
        }
    }
}
