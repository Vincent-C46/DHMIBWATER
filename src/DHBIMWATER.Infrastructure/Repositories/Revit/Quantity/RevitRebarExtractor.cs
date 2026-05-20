using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Quantity
{
    public class RevitRebarExtractor : IQuantityExtractor
    {
        private readonly Func<Document?> _doc;
        public RevitRebarExtractor(Func<Document?> doc)
        {
            _doc = doc;
        }
        public IEnumerable<long> CollectElementIds()
            => new FilteredElementCollector(_doc()).OfCategory(BuiltInCategory.OST_Rebar).WhereElementIsNotElementType().Select(r => r.Id.Value);   

        public bool CanExtract(long elementId)
        {
            var doc = _doc();
            if (doc == null) return false;

            var elem = doc.GetElement(new ElementId(elementId));

            return elem is Rebar;
        }

        public IEnumerable<QuantityItem> Extract(long elementId)
        {
            var doc = _doc();
            if (doc == null)
                return Enumerable.Empty<QuantityItem>();

            var rebar = (Rebar)doc.GetElement(new ElementId(elementId));

            // 객체 추출값
            var length = Math.Round(UC.Ft2ToM2(rebar.get_Parameter(BuiltInParameter.REBAR_ELEM_LENGTH).AsDouble()), 2);
            int count = rebar.get_Parameter(BuiltInParameter.REBAR_ELEM_QUANTITY_OF_BARS).AsInteger();
            double unitWeight = 1;

            var varDict = new Dictionary<string, double>
            {
                ["L"] = length,
                ["N"] = count,
                ["UW"]  = unitWeight,
            };

            var concFormula = "A x Thk";
            //var concFormula = "A * Thk";
            string? concRendered = FormulaCalculator.Render(concFormula, varDict);
            double concValue = FormulaCalculator.Calculate(concFormula, varDict);

            var quantityItems = new List<QuantityItem>();

            // 콘크리트
            var rebarItem = new QuantityItem
            {
                ElementId = elementId,
                Category = rebar.LookupParameter("DH_Category")?.AsString() ?? string.Empty,
                ElementCode = rebar.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                WorkType = "철근콘크리트",
                Specification = "25-18-250",
                Material = string.Empty,
                Formula = concRendered,
                Value = concValue,
                Unit = "m³"
            };

            var listToAdd = new List<QuantityItem>() { rebarItem, };
            quantityItems.AddRange(listToAdd);

            return quantityItems;
        }
    }
}
