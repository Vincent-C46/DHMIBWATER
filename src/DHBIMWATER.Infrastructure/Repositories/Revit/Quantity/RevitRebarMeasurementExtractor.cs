using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Core.Structures;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Quantity
{
    public class RevitRebarMeasurementExtractor : IElementMeasurementExtractor
    {
        private readonly Func<Document?> _doc;

        public RevitRebarMeasurementExtractor(Func<Document?> doc)
        {
            _doc = doc;
        }

        public bool CanExtract(long elementId)
        {
            var doc = _doc();
            if (doc == null) return false;
            return doc.GetElement(new ElementId(elementId)) is Rebar;
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

        public ElementMeasurements Extract(long elementId)
        {
            var doc = _doc();
            var empty = new ElementMeasurements { ElementId = elementId };
            if (doc == null) return empty;

            var rebar = doc.GetElement(new ElementId(elementId)) as Rebar;
            if (rebar == null) return empty;

            var length = UC.FtToM(rebar.get_Parameter(BuiltInParameter.REBAR_ELEM_LENGTH)?.AsDouble() ?? 0);
            int count = rebar.get_Parameter(BuiltInParameter.REBAR_ELEM_QUANTITY_OF_BARS)?.AsInteger() ?? 0;
            string typeName = rebar.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM)?.AsValueString() ?? string.Empty;

            var spliceNumParam = rebar.Parameters.Cast<Parameter>()
                .FirstOrDefault(p => { var n = p.Definition?.Name ?? ""; return n.Contains("이음") && n.Contains("개수"); });
            var spliceLenParam = rebar.Parameters.Cast<Parameter>()
                .FirstOrDefault(p => { var n = p.Definition?.Name ?? ""; return n.Contains("이음") && n.Contains("길이"); });

            int spliceNum = spliceNumParam?.AsInteger() ?? 0;
            double spliceLen = UC.FtToM(spliceLenParam?.AsDouble() ?? 0);
            double totalLength = length + spliceNum * spliceLen;

            var key = RebarDatabase.KSD3504.All.Keys.FirstOrDefault(k => typeName.Contains(k));
            if (key == null) return empty;

            var rebarSpec = RebarDatabase.KSD3504.Get(key);

            var builtInCategory = rebar.Category?.Name ?? string.Empty;
            var dhCategory = rebar.LookupParameter("DH_Category")?.AsString();
            var category = !string.IsNullOrEmpty(dhCategory) ? dhCategory : builtInCategory;

            return new ElementMeasurements
            {
                ElementId  = elementId,
                Category   = category,
                CategoryId = (int)BuiltInCategory.OST_Rebar,
                Values = new Dictionary<string, double>
                {
                    ["L"]  = totalLength,
                    ["N"]  = count,
                    ["UW"] = rebarSpec.UnitWeightKgPerM,
                },
                Parameters = new Dictionary<string, string>
                {
                    ["RebarKey"]       = key,
                    ["DH_ElementCode"] = rebar.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                }
            };
        }
    }
}
