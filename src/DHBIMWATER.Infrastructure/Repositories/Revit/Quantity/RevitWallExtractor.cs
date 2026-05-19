using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.Interfaces;
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
            //TaskDialog.Show("Debug", elem?.GetType().Name ?? "null");

            return elem is Wall;
        }

        public IEnumerable<QuantityItem> Extract(long elementId)
        {
            var doc = _doc();
            if (doc == null)
                return Enumerable.Empty<QuantityItem>();

            var wall = (Wall)doc.GetElement(new ElementId(elementId));
            var cs = wall.WallType.GetCompoundStructure();

            // 객체 추출값
            var area = Math.Round(UC.Ft2ToM2(wall.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED).AsDouble()), 2);
            double thickness = Math.Round(UC.FtToM(cs.GetLayers()
                                          .Where(l => l.Function == MaterialFunctionAssignment.Structure)
                                          .Sum(l => l.Width)), 2);


            var varDict = new Dictionary<string, double>
            {
                ["A"] = area,
                ["Thk"] = thickness,
            };

            //var concFormula = "A x Thk";
            var concFormula = "A * Thk";
            string? concRendered = FormulaCalculator.Render(concFormula, varDict);
            double concValue = FormulaCalculator.Calculate(concFormula, varDict);

            var quantityItems = new List<QuantityItem>();

            // 콘크리트
            var concreteItem = new QuantityItem
            {
                ElementId = elementId,
                Category = wall.LookupParameter("DH_Category")?.ToString() ?? string.Empty,
                ElementCode = wall.LookupParameter("DH_ElementCode")?.ToString() ?? string.Empty,
                WorkType = "철근콘크리트",
                Specification = "25-18-250",
                Material = string.Empty,
                Formula = concRendered,
                Value = concValue,
                Unit = "m³"
            };

            // 거푸집
            var exteriorFormItem = new QuantityItem
            {
                ElementId = elementId,
                Category = wall.LookupParameter("DH_Category")?.ToString() ?? string.Empty,
                ElementCode = wall.LookupParameter("DH_ElementCode")?.ToString() ?? string.Empty,
                WorkType ="거푸집",
                Specification = "유로폼",
                Material = string.Empty,
                Formula = $"{area}(A)",
                Value = area,
                Unit = "m²"
            };

            var interiorFormItem = new QuantityItem
            {
                ElementId = elementId,
                Category = wall.LookupParameter("DH_Category")?.ToString() ?? string.Empty,
                ElementCode = wall.LookupParameter("DH_ElementCode")?.ToString() ?? string.Empty,
                WorkType = "벽",
                Specification = "내측 거푸집",
                Material = string.Empty,
                Formula = $"{area}(A)",
                Value = area,
                Unit = "m²"
            };

            var listToAdd = new List<QuantityItem>() { concreteItem, exteriorFormItem, interiorFormItem };
            quantityItems.AddRange(listToAdd);

            return quantityItems;
        }
    }
}
