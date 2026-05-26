using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Core.Structures;
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
        {
            var doc = _doc();
            if (doc == null) return Enumerable.Empty<long>();

            return new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_Rebar)
                        .WhereElementIsNotElementType()
                        .Select(r => r.Id.Value);
        }

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

            var rebar = doc.GetElement(new ElementId(elementId)) as Rebar;
            if (rebar == null)
                return Enumerable.Empty<QuantityItem>();

            var quantityItems = new List<QuantityItem>();

            // 객체 추출값
            var length = UC.FtToM(rebar.get_Parameter(BuiltInParameter.REBAR_ELEM_LENGTH)?.AsDouble() ?? 0);
            int count = rebar.get_Parameter(BuiltInParameter.REBAR_ELEM_QUANTITY_OF_BARS).AsInteger();
            string typeName = rebar.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).AsValueString() ?? string.Empty;
            long hostId = rebar.GetHostId().Value;

            var rebarDict = RebarDatabase.KSD3504.All;  // KS D 3504 철근 규격
            var key = rebarDict.Keys.FirstOrDefault(k => typeName.Contains(k));

            if (key == null)
                return Enumerable.Empty<QuantityItem>();

            var rebarSpec = RebarDatabase.KSD3504.Get(key);

            var varDict = new Dictionary<string, double>
            {
                ["L"] = length,
                ["N"] = count,
                ["UW"]  = rebarSpec.UnitWeightKgPerM,
            };

            const string rebarFormula = "L x N x UW / 1000";
            string? rebarRendered = FormulaCalculator.Render(rebarFormula, varDict);
            double rebarValue = FormulaCalculator.Calculate(rebarFormula, varDict);

            // 철근
            var rebarItem = new QuantityItem
            {
                HostElementId = hostId,
                ElementId = elementId,
                Category = rebar.LookupParameter("DH_Category")?.AsString() ?? string.Empty,
                ElementCode = rebar.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                WorkType = "철근",
                Specification = key,
                RawFormula = rebarFormula,
                RenderedFormula = rebarRendered,
                Value = rebarValue,
                Unit = "t"
            };

            var listToAdd = new List<QuantityItem>() { rebarItem, };
            quantityItems.AddRange(listToAdd);

            return quantityItems;
        }
    }
}
