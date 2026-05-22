using Autodesk.Revit.DB;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Infrastructure.Helpers;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;


namespace DHBIMWATER.Infrastructure.Repositories.Revit.Quantity
{
    public class RevitBeamExtractor : IQuantityExtractor
    {
        private readonly Func<Document?> _doc;

        public RevitBeamExtractor(Func<Document?> doc)
        {
            _doc = doc;
        }

        public bool CanExtract(long elementId)
        {
            var doc = _doc();
            if (doc == null) return false;

            var elem = doc.GetElement(new ElementId(elementId));

            return elem is FamilyInstance fi && fi.Category.Id.Value == (int)BuiltInCategory.OST_StructuralFraming;
        }

        public IEnumerable<long> CollectElementIds()
        {
            var doc = _doc();
            if (doc == null) return Enumerable.Empty<long>();

            return new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_StructuralFraming)
                        .WhereElementIsNotElementType()
                        .Select(r => r.Id.Value);
        }

        public IEnumerable<QuantityItem> Extract(long elementId)
        {
            var doc = _doc();
            if (doc == null)
                return Enumerable.Empty<QuantityItem>();

            var beam = doc.GetElement(new ElementId(elementId)) as FamilyInstance;

            if (beam == null)
                return Enumerable.Empty<QuantityItem>();

            var quantityItems = new List<QuantityItem>();

            // 객체 추출값
            var length = UC.FtToM(beam.get_Parameter(BuiltInParameter.INSTANCE_LENGTH_PARAM)?.AsDouble() ?? 0);

            var b = UC.FtToM(FamilyInstanceHelper.FindParameter(beam, "b") ?? 0) ;
            var d = UC.FtToM(FamilyInstanceHelper.FindParameter(beam, "d") ?? 0) ;
            var h = UC.FtToM(FamilyInstanceHelper.FindParameter(beam, "h") ?? 0) ;
            string typeName = beam.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).AsValueString() ?? string.Empty;

            //string materialName = string.Empty;
            //var materialId = beam.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM)?.AsElementId();
            
            //if (materialId == null || materialId == ElementId.InvalidElementId)
            //    materialName = string.Empty;
            //else
            //    materialName = (doc.GetElement(materialId) as Material).Name;

            var varDict = new Dictionary<string, double>
            {
                ["L"] = length,
                ["B"] = b,
                ["H"] = h,
            };

            const string concFormula = "B x H x L";
            string? concRendered = FormulaCalculator.Render(concFormula, varDict);
            double concValue = FormulaCalculator.Calculate(concFormula, varDict);

            // 철근콘크리트
            var concreteItem = new QuantityItem
            {
                ElementId = elementId,
                Category = beam.Category.Name ?? string.Empty,
                ElementCode = beam.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                WorkType = "철근콘크리트",
                Specification = typeName,
                RawFormula = concFormula,
                RenderedFormula = concRendered,
                Value = concValue,
                Unit = "m³"
            };

            var listToAdd = new List<QuantityItem>() { concreteItem, };
            quantityItems.AddRange(listToAdd);

            return quantityItems;
        }
    }
}
