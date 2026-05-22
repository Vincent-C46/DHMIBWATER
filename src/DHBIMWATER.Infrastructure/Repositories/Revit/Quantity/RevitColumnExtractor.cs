using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Core.Structures;
using DHBIMWATER.Infrastructure.Helpers;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Quantity
{
    public class RevitColumnExtractor : IQuantityExtractor
    {
        private readonly Func<Document?> _doc;
        public RevitColumnExtractor(Func<Document?> doc)
        {
            _doc = doc;
        }

        public bool CanExtract(long elementId)
        {
            var doc = _doc();
            if (doc == null) return false;

            var elem = doc.GetElement(new ElementId(elementId));

            return elem is FamilyInstance fi && fi.Category.Id.Value == (int)BuiltInCategory.OST_StructuralColumns;
        }

        public IEnumerable<long> CollectElementIds()
        {
            var doc = _doc();
            if (doc == null) return Enumerable.Empty<long>();

            return new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_StructuralColumns)
            .WhereElementIsNotElementType()
            .Select(r => r.Id.Value);
        }

        public IEnumerable<QuantityItem> Extract(long elementId)
        {
            var doc = _doc();
            if (doc == null)
                return Enumerable.Empty<QuantityItem>();

            var column = doc.GetElement(new ElementId(elementId)) as FamilyInstance;

            if (column == null)
                return Enumerable.Empty<QuantityItem>();

            var quantityItems = new List<QuantityItem>();

            // 객체 추출값
            var length = UC.FtToM(column.get_Parameter(BuiltInParameter.INSTANCE_LENGTH_PARAM)?.AsDouble() ?? 0);
            var b = UC.FtToM(FamilyInstanceHelper.FindParameter(column, "b") ?? 0);
            var d = UC.FtToM(FamilyInstanceHelper.FindParameter(column, "d") ?? 0);
            var h = UC.FtToM(FamilyInstanceHelper.FindParameter(column, "h") ?? 0);
            var r = UC.FtToM(FamilyInstanceHelper.FindParameter(column, "r") ?? 0);

            string typeName = column.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).AsValueString() ?? string.Empty;



            var varDict = new Dictionary<string, double>
            {
                ["L"] = length,
                ["B"] = b,
                ["H"] = h,
                ["D"] = d,
            };

            string concFormula = "B x H x L";
            string? columnRendered = FormulaCalculator.Render(concFormula, varDict);
            double columnValue = FormulaCalculator.Calculate(concFormula, varDict);

            // 철근콘크리트
            var concreteItem = new QuantityItem
            {
                ElementId = elementId,
                Category = column.Category.Name ?? string.Empty,
                ElementCode = column.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                WorkType = "철근콘크리트",
                Specification = typeName,
                RawFormula = concFormula,
                RenderedFormula = columnRendered ?? string.Empty,
                Value = columnValue,
                Unit = "m³"
            };

            var listToAdd = new List<QuantityItem>() { concreteItem, };
            quantityItems.AddRange(listToAdd);

            return quantityItems;
        }
    }
}
