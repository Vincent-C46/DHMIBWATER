using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using DHBIMWATER.Application.Interfaces.Geometry;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Infrastructure.Helpers;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Quantity
{
    public class RevitFoundationMeasurementExtractor : IElementMeasurementExtractor
    {
        private readonly Func<Document?> _doc;
        private readonly IIntersectingElementFinder _finder;
        private readonly IFaceClassifier _classifier;

        public RevitFoundationMeasurementExtractor(Func<Document?> doc, IIntersectingElementFinder finder, IFaceClassifier classifier)
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
            return elem is FamilyInstance fi && fi.Category.Id.Value == (int)BuiltInCategory.OST_StructuralFoundation;
        }

        public IEnumerable<long> CollectElementIds()
        {
            var doc = _doc();
            if (doc == null) return Enumerable.Empty<long>();

            // OfClass(FamilyInstance) → Floor 기반 줄기초/온통기초 자동 제외
            return new FilteredElementCollector(doc)
                .OfClass(typeof(FamilyInstance))
                .OfCategory(BuiltInCategory.OST_StructuralFoundation)
                .WhereElementIsNotElementType()
                .Select(r => r.Id.Value);
        }

        public ElementMeasurements Extract(long elementId)
        {
            var doc = _doc();
            var empty = new ElementMeasurements { ElementId = elementId };
            if (doc == null) return empty;

            var fnd = doc.GetElement(new ElementId(elementId)) as FamilyInstance;
            if (fnd == null) return empty;

            var refFaceDict = _classifier.GetFaceAreas(elementId);
            var deductionByFaceType = QuantityExtractorHelper.GroupDeductions(_finder.FindContactAreas(elementId));

            // ── 치수 파라미터 ──────────────────────────────────────────────
            var b = UC.FtToM(FamilyInstanceHelper.FindParameter(fnd, "b") ??
                             FamilyInstanceHelper.FindParameter(fnd, "w") ??
                             FamilyInstanceHelper.FindParameter(fnd, "width") ??
                             FamilyInstanceHelper.FindParameter(fnd, "폭") ?? 0);
            var d = UC.FtToM(FamilyInstanceHelper.FindParameter(fnd, "d") ??
                             FamilyInstanceHelper.FindParameter(fnd, "l") ??
                             FamilyInstanceHelper.FindParameter(fnd, "Length") ??
                             FamilyInstanceHelper.FindParameter(fnd, "길이") ?? 0);
            var h = UC.FtToM(FamilyInstanceHelper.FindParameter(fnd, "h") ??
                             FamilyInstanceHelper.FindParameter(fnd, "Thickness") ??
                             FamilyInstanceHelper.FindParameter(fnd, "Thk") ??
                             FamilyInstanceHelper.FindParameter(fnd, "두께") ??
                             FamilyInstanceHelper.FindParameter(fnd, "기초 두께") ??
                             FamilyInstanceHelper.FindParameter(fnd, "Height") ?? 0);

            double volume = UC.Ft3ToM3(RevitGeometryHelper.GetSolids(fnd).Sum(s => s.Volume));

            // ── 재료 정보 ─────────────────────────────────────────────────
            var materialId = fnd.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM)?.AsElementId();
            if (materialId == null || materialId == ElementId.InvalidElementId)
                materialId = fnd.Symbol.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM)?.AsElementId();

            var materialName = (materialId != null && materialId != ElementId.InvalidElementId)
                ? (doc.GetElement(materialId) as Material)?.Name ?? string.Empty
                : string.Empty;

            var materialClass    = FamilyInstanceHelper.GetStructuralAssetClass(fnd);
            var materialClassStr = materialClass switch
            {
                StructuralAssetClass.Metal   => "강재",
                StructuralAssetClass.Generic => "기타",
                _                            => "콘크리트"
            };

            string concWorkType = materialClassStr == "콘크리트"
                ? (materialName.Contains("무근") || h < 0.15 ? "무근콘크리트" : "철근콘크리트")
                : string.Empty;

            return new ElementMeasurements
            {
                ElementId  = elementId,
                Category   = fnd.Category.Name ?? string.Empty,
                CategoryId = (int)BuiltInCategory.OST_StructuralFoundation,
                Values = new Dictionary<string, double>
                {
                    ["B"]            = b,
                    ["D"]            = d,
                    ["H"]            = h,
                    ["Vol"]          = volume,
                    ["A_side_gross"] = refFaceDict.GetValueOrDefault(FaceType.Side, 0),
                    ["A_side_net"]   = QuantityExtractorHelper.GetNetArea(refFaceDict, deductionByFaceType, FaceType.Side),
                },
                Parameters = new Dictionary<string, string>
                {
                    ["MaterialClass"]  = materialClassStr,
                    ["ConcWorkType"]   = concWorkType,
                    ["MaterialName"]   = materialName,
                    ["DH_ElementCode"] = fnd.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                }
            };
        }
    }
}
