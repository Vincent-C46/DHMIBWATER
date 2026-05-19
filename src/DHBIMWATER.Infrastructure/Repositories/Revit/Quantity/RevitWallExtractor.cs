using Autodesk.Revit.DB;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Parameters;
using DHBIMWATER.Core.Quantity;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Quantity
{
    public class RevitWallExtractor : IQuantityExtractor
    {
        private readonly Func<Document?> _doc;
        public RevitWallExtractor(Func<Document?> doc)
        {
            _doc = doc;
        }

        public bool CanExtract(long elementId)
        {
            var doc = _doc();
            if (doc == null) return false;

            var elem = doc.GetElement(new ElementId(elementId));

            return elem is Wall;
        }

        public IEnumerable<QuantityItem> Extract(long elementId)
        {
            var doc = _doc();
            if (doc == null)
                return Enumerable.Empty<QuantityItem>();

            var wall = (Wall)doc.GetElement(new ElementId(elementId));
            var area = UC.Ft2ToM2(wall.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED).AsDouble());

            var cs = wall.WallType.GetCompoundStructure();
            double thickness = UC.FtToM(cs.GetLayers()
                                          .Where(l => l.Function == MaterialFunctionAssignment.Structure)
                                          .Sum(l => l.Width));
            var quantityItems = new List<QuantityItem>();

            // 콘크리트
            var concreteItem = new QuantityItem
            {
                ElementId = elementId,
                Category = wall.LookupParameter("DH_Category").ToString(),
                ElementCode = wall.LookupParameter("DH_ElementCode").ToString(),
                WorkType = "벽",
                Specification = "철근 콘크리트",
                Material = string.Empty,
                Formula = $"{area}(A) * {thickness}(Thk)",
                Value = area * thickness,
                Unit = "m²"
            };

            // 거푸집
            var exteriorFormItem = new QuantityItem
            {
                ElementId = elementId,
                Category = wall.LookupParameter("DH_Category").ToString(),
                ElementCode = wall.LookupParameter("DH_ElementCode").ToString(),
                WorkType = "벽",
                Specification = "외측 거푸집",
                Material = string.Empty,
                Formula = $"{area}(A)",
                Value = area,
                Unit = "m²"
            };

            var interiorFormItem = new QuantityItem
            {
                ElementId = elementId,
                Category = wall.LookupParameter("DH_Category").ToString(),
                ElementCode = wall.LookupParameter("DH_ElementCode").ToString(),
                WorkType = "벽",
                Specification = "내측 거푸집",
                Material = string.Empty,
                Formula = $"{area}(A)",
                Value = area,
                Unit = "m²"
            };

            var listToAdd = new List<QuantityItem>() { concreteItem, };
            quantityItems.AddRange(listToAdd);

            return quantityItems;
        }
    }
}
