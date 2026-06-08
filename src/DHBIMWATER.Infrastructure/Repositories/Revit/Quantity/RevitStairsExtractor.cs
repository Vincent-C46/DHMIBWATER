using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.Interfaces.Geometry;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Infrastructure.Helpers;
using System.Data.Common;
using System.Diagnostics;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Quantity
{
    public class RevitStairsExtractor : IQuantityExtractor
    {
        private readonly Func<Document?> _doc;
        private readonly IIntersectingElementFinder _finder;

        public RevitStairsExtractor(Func<Document?> doc, IIntersectingElementFinder finder)
        {
            _doc = doc;
            _finder = finder;
        }
        public bool CanExtract(long elementId)
        {
            var doc = _doc();
            if (doc == null) return false;

            var elem = doc.GetElement(new ElementId(elementId));

            return elem is Stairs;
        }

        public IEnumerable<long> CollectElementIds()
        {
            var doc = _doc();
            if (doc == null) return Enumerable.Empty<long>();

            return new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_Stairs)
                        .WhereElementIsNotElementType()
                        .Select(r => r.Id.Value);
        }

        public IEnumerable<QuantityItem> Extract(long elementId)
        {
            var doc = _doc();
            if (doc == null) return Enumerable.Empty<QuantityItem>();
            
            var stair = doc.GetElement(new ElementId(elementId)) as Stairs;
            if (stair == null) return Enumerable.Empty<QuantityItem>();

            var intersectingAreas = _finder.FindContactAreas(elementId);
            var quantityItems = new List<QuantityItem>();

            Debug.WriteLine($"{stair.Id.Value}");

            //string materialName = string.Empty;
            //var materialId = stair.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM)?.AsElementId()
            //                    ?? stair.Document.GetElement(stair.GetTypeId()).get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM)?.AsElementId();

            //if (materialId == null || materialId == ElementId.InvalidElementId)
            //    materialName = string.Empty;
            //else
            //    materialName = (doc.GetElement(materialId) as Material).Name;

            var varDict = new Dictionary<string, double>
            {
                ["N"] = 2,
            };

            string concFormula = "2 * N";
            string? concRendered = FormulaCalculator.Render(concFormula, varDict);
            double concValue = UC.Ft3ToM3(RevitGeometryHelper.GetSolids(stair).Sum(s => s.Volume));

            // 철근콘크리트
            var concreteItem = new QuantityItem
            {
                ElementId = elementId,
                Category = stair.Category.Name ?? "계단",
                ElementCode = stair.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                WorkType = "철근콘크리트",
                Specification = "materialName",
                RawFormula = concFormula,
                RenderedFormula = concRendered ?? string.Empty,
                Value = concValue,
                Unit = "m³"
            };
            quantityItems.Add(concreteItem);

            return quantityItems;
        }
    }
}
