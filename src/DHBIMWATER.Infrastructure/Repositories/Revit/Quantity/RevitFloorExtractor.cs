using Autodesk.Revit.DB;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Quantity
{
    public class RevitFloorExtractor : IQuantityExtractor
    {
        private readonly Func<Document?> _doc;
        public RevitFloorExtractor(Func<Document?> doc)
        {
            _doc = doc;
        }

        public bool CanExtract(long elementId)
        {
            var doc = _doc();
            if (doc == null) return false;

            var elem = doc.GetElement(new ElementId(elementId));
            //TaskDialog.Show("Debug", elem?.GetType().Name ?? "null");

            return elem is Floor;
        }

        public IEnumerable<long> CollectElementIds()
            => new FilteredElementCollector(_doc()).OfClass(typeof(Floor)).WhereElementIsNotElementType().Select(w => w.Id.Value);


        public IEnumerable<QuantityItem> Extract(long elementId)
        {
            var doc = _doc();
            if (doc == null)
                return Enumerable.Empty<QuantityItem>();

            var floor = (Floor)doc.GetElement(new ElementId(elementId));
            var cs = floor.FloorType.GetCompoundStructure();

            var quantityItems = new List<QuantityItem>();

            // 객체 추출값
            var area = Math.Round(UC.Ft2ToM2(floor.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED).AsDouble()), 2);
            double thickness = Math.Round(UC.FtToM(cs.GetLayers()
                                          .Where(l => l.Function == MaterialFunctionAssignment.Structure)
                                          .Sum(l => l.Width)), 2);

            var varDict = new Dictionary<string, double>
            {
                ["A"] = area,
                ["Thk"] = thickness,
            };

            var concFormula = "A x Thk";

            //var concFormula = "A * Thk";
            string? concRendered = FormulaCalculator.Render(concFormula, varDict);
            double concValue = FormulaCalculator.Calculate(concFormula, varDict);

            var formFormula = "A";
            string? formRendered = FormulaCalculator.Render(formFormula, varDict);
            double formValue = FormulaCalculator.Calculate(formFormula, varDict);


            // 콘크리트
            var concreteItem = new QuantityItem
            {
                ElementId = elementId,
                Category = floor.LookupParameter("DH_Category")?.AsString() ?? string.Empty,
                ElementCode = floor.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                WorkType = "철근콘크리트",
                Specification = "25-18-250",
                Material = string.Empty,
                Formula = concRendered,
                Value = concValue,
                Unit = "m³"
            };

            // 거푸집
            var bottomFormItem = new QuantityItem
            {
                ElementId = elementId,
                Category = floor.LookupParameter("DH_Category")?.AsString() ?? string.Empty,
                ElementCode = floor.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                WorkType = "거푸집",
                Specification = "합판 4회",
                Material = string.Empty,
                Formula = formRendered,
                Value = formValue,
                Unit = "m²"
            };

            var listToAdd = new List<QuantityItem>() { concreteItem, bottomFormItem, };
            quantityItems.AddRange(listToAdd);

            return quantityItems;
        }
    }
}
