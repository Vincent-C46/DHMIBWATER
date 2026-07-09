using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Infrastructure.Helpers;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Quantity
{
    public class RevitGenericModelExtractor : IQuantityExtractor
    {
        private readonly Func<Document?> _doc;
        private HashSet<long>? _rebarHostIds;

        public RevitGenericModelExtractor(Func<Document?> doc)
        {
            _doc = doc;
        }

        public bool CanExtract(long elementId)
        {
            var doc = _doc();
            if (doc == null) return false;

            var elem = doc.GetElement(new ElementId(elementId));
            if (elem is not FamilyInstance fi) return false;
            if (fi.Category.Id.Value != (long)BuiltInCategory.OST_GenericModel) return false;

            var placementType = fi.Symbol.Family.FamilyPlacementType;

            return placementType switch
            {
                FamilyPlacementType.CurveBased            => false,
                FamilyPlacementType.CurveBasedDetail      => false,
                FamilyPlacementType.TwoLevelsBased        => false,
                FamilyPlacementType.ViewBased             => false,
                FamilyPlacementType.Adaptive              => false,
                FamilyPlacementType.Invalid               => false,
                FamilyPlacementType.CurveDrivenStructural => false,
                _ => (fi.get_Parameter(BuiltInParameter.HOST_VOLUME_COMPUTED)?.AsDouble() ?? 0) > 0,
            };
        }

        public IEnumerable<long> CollectElementIds()
        {
            var doc = _doc();
            if (doc == null) return Enumerable.Empty<long>();

            return new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_GenericModel)
                .WhereElementIsNotElementType()
                .Select(r => r.Id.Value);
        }

        public IEnumerable<QuantityItem> Extract(long elementId)
        {
            var doc = _doc();
            if (doc == null) return Enumerable.Empty<QuantityItem>();

            var generic = doc.GetElement(new ElementId(elementId)) as FamilyInstance;
            if (generic == null) return Enumerable.Empty<QuantityItem>();

            // StructuralAssetClass.Concrete 인 경우에만 수량 산출
            var materialId = FamilyInstanceHelper.GetMaterialId(generic);
            if (!FamilyInstanceHelper.IsConcreteMaterial(doc, materialId))
                return Enumerable.Empty<QuantityItem>();

            string materialName = (doc.GetElement(materialId) as Material)?.Name ?? string.Empty;

            double concValue = UC.Ft3ToM3(RevitGeometryHelper.GetSolids(generic).Sum(s => s.Volume));
            if (concValue <= 1e-6) return Enumerable.Empty<QuantityItem>();

            // 귀속 철근 유무 → 철근콘크리트 / 무근콘크리트
            bool hasRebar = GetRebarHostIds(doc).Contains(elementId);
            string workType = hasRebar ? "철근콘크리트" : "무근콘크리트";

            var varDict = new Dictionary<string, double>
            {
                ["V"] = concValue,
                ["N"] = 1,
            };

            string typeName = doc.GetElement(generic.GetTypeId()).Name;

            var numItem = new QuantityItem
            {
                ElementId       = elementId,
                Category        = generic.LookupParameter("DH_Category")?.AsString() ?? string.Empty,
                ElementCode     = generic.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                RawFormula      = "N",
                RenderedFormula = FormulaCalculator.Render("N", varDict),
                WorkType        = typeName,
                Specification   = materialName,
                Value           = 1,
                Unit            = "EA"
            };

            var concItem = new QuantityItem
            {
                ElementId       = elementId,
                Category        = generic.Category.Name ?? string.Empty,
                ElementCode     = generic.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                WorkType        = workType,
                Specification   = materialName,
                RawFormula      = "V",
                RenderedFormula = FormulaCalculator.Render("V", varDict),
                Value           = concValue,
                Unit            = "m³"
            };

            return [numItem, concItem];
        }

        // 문서 내 모든 철근의 호스트 ID를 1회 수집 → 이후 O(1) 조회
        private HashSet<long> GetRebarHostIds(Document doc)
        {
            if (_rebarHostIds != null) return _rebarHostIds;

            _rebarHostIds = new FilteredElementCollector(doc)
                .OfClass(typeof(Rebar))
                .Cast<Rebar>()
                .Select(r => r.GetHostId())
                .Where(id => id != null && id != ElementId.InvalidElementId)
                .Select(id => id.Value)
                .ToHashSet();

            return _rebarHostIds;
        }
    }
}
