using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Infrastructure.Helpers;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Quantity
{
    public class RevitDirectShapeExtractor : IQuantityExtractor
    {
        private readonly Func<Document?> _doc;
        private HashSet<long>? _rebarHostIds;

        public RevitDirectShapeExtractor(Func<Document?> doc)
        {
            _doc = doc;
        }

        public bool CanExtract(long elementId)
        {
            var doc = _doc();
            if (doc == null) return false;
            return doc.GetElement(new ElementId(elementId)) is DirectShape;
        }

        public IEnumerable<long> CollectElementIds()
        {
            var doc = _doc();
            if (doc == null) return Enumerable.Empty<long>();

            return new FilteredElementCollector(doc)
                .OfClass(typeof(DirectShape))
                .WhereElementIsNotElementType()
                .Select(r => r.Id.Value);
        }

        public IEnumerable<QuantityItem> Extract(long elementId)
        {
            var doc = _doc();
            if (doc == null) return Enumerable.Empty<QuantityItem>();

            var ds = doc.GetElement(new ElementId(elementId)) as DirectShape;
            if (ds == null) return Enumerable.Empty<QuantityItem>();

            // StructuralAssetClass.Concrete 인 경우에만 수량 산출
            var materialId = ds.GetMaterialIds(false).FirstOrDefault();
            if (!FamilyInstanceHelper.IsConcreteMaterial(doc, materialId))
                return Enumerable.Empty<QuantityItem>();

            string materialName = (doc.GetElement(materialId) as Material)?.Name ?? string.Empty;

            double volume = UC.Ft3ToM3(RevitGeometryHelper.GetSolids(ds).Sum(s => s.Volume));
            if (volume <= 1e-6) return Enumerable.Empty<QuantityItem>();

            // 귀속 철근 유무 → 철근콘크리트 / 무근콘크리트
            bool hasRebar = GetRebarHostIds(doc).Contains(elementId);
            string workType = hasRebar ? "철근콘크리트" : "무근콘크리트";

            var varDict = new Dictionary<string, double> { ["V"] = volume };

            var concItem = new QuantityItem
            {
                ElementId       = elementId,
                Category        = ds.LookupParameter("DH_Category")?.AsString() ?? string.Empty,
                ElementCode     = ds.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                WorkType        = workType,
                Specification   = materialName,
                RawFormula      = "V",
                RenderedFormula = FormulaCalculator.Render("V", varDict),
                Value           = volume,
                Unit            = "m³"
            };

            return [concItem];
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
