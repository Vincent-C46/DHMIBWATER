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

            string materialName = string.Empty;
            var materialId = column.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM)?.AsElementId()
                                ?? column.Document.GetElement(column.GetTypeId()).get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM)?.AsElementId();

            if (materialId == null || materialId == ElementId.InvalidElementId)
                materialName = string.Empty;
            else
                materialName = (doc.GetElement(materialId) as Material).Name;


            // 객체 추출값
            var length = UC.FtToM(column.get_Parameter(BuiltInParameter.INSTANCE_LENGTH_PARAM)?.AsDouble() ?? 0);
            var b = UC.FtToM(FamilyInstanceHelper.FindParameter(column, "b") ?? 0);
            var d = UC.FtToM(FamilyInstanceHelper.FindParameter(column, "d") ??
                             FamilyInstanceHelper.FindParameter(column, "h") ??
                             0);
            var r = UC.FtToM(FamilyInstanceHelper.FindParameter(column, "r") ?? 0);

            string typeName = column.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).AsValueString() ?? string.Empty;
            bool isCircular = typeName.Contains("원형", StringComparison.OrdinalIgnoreCase) || typeName.Contains("circular", StringComparison.OrdinalIgnoreCase);

            var varDict = new Dictionary<string, double>
            {
                ["B"] = b,
                ["D"] = d,
                ["R"] = r,
                ["L"] = length,
            };

            string concFormula = isCircular ? "R^2 x L" : "B x D x L";
            string? columnRendered = FormulaCalculator.Render(concFormula, varDict);
            double columnValue = FormulaCalculator.Calculate(concFormula, varDict);

            // 철근콘크리트
            var concreteItem = new QuantityItem
            {
                ElementId = elementId,
                Category = column.Category.Name ?? string.Empty,
                ElementCode = column.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                WorkType = "철근콘크리트",
                Specification = materialName,
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
