using Autodesk.Revit.DB;
using DHBIMWATER.Application.Interfaces.Geometry;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Infrastructure.Helpers;
using System.Diagnostics;
using System.Reflection;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;


namespace DHBIMWATER.Infrastructure.Repositories.Revit.Quantity
{
    public class RevitBeamExtractor : IQuantityExtractor
    {
        private readonly Func<Document?> _doc;
        private readonly IIntersectingElementFinder _finder;


        public RevitBeamExtractor(Func<Document?> doc, IIntersectingElementFinder finder)
        {
            _doc = doc;
            _finder = finder;
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
            var intersectingIds = _finder.FindIntersecting(elementId);

            if (beam == null)
                return Enumerable.Empty<QuantityItem>();

            var quantityItems = new List<QuantityItem>();

            // 객체 추출값
            var length = UC.FtToM(beam.get_Parameter(BuiltInParameter.INSTANCE_LENGTH_PARAM)?.AsDouble() ?? 0);

            var b = UC.FtToM(FamilyInstanceHelper.FindParameter(beam, "b") ?? FamilyInstanceHelper.FindParameter(beam, "width") ?? FamilyInstanceHelper.FindParameter(beam, "폭") ??0) ;
            var h = UC.FtToM(FamilyInstanceHelper.FindParameter(beam, "h") ?? FamilyInstanceHelper.FindParameter(beam, "d") ?? FamilyInstanceHelper.FindParameter(beam, "높이") ?? FamilyInstanceHelper.FindParameter(beam, "Height") ?? 0) ;
            string typeName = beam.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).AsValueString() ?? string.Empty;

            string materialName = string.Empty;
            var materialId = beam.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM)?.AsElementId()
                                ?? beam.Document.GetElement(beam.GetTypeId())?.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM)?.AsElementId();

            if (materialId == null || materialId == ElementId.InvalidElementId)
                materialName = string.Empty;
            else
                materialName = (doc.GetElement(materialId) as Material).Name;

            var varDict = new Dictionary<string, double>
            {
                ["B"] = b,
                ["H"] = h,
                ["L"] = length,
            };

            const string concFormula = "B x H x L";
            string? concRendered = FormulaCalculator.Render(concFormula, varDict);
            //double concValue = FormulaCalculator.Calculate(concFormula, varDict);
            double concValue = UC.Ft3ToM3(RevitGeometryHelper.GetSolids(beam).Sum(s => s.Volume));

            // 철근콘크리트
            var concreteItem = new QuantityItem
            {
                ElementId = elementId,
                Category = beam.Category.Name ?? string.Empty,
                ElementCode = beam.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                WorkType = "철근콘크리트",
                Specification = materialName,
                //SubSpecification = $"{a:F3} m³",
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
