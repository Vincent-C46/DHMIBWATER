using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Infrastructure.Helpers;
using System.Windows;
using System.Windows.Controls;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Quantity
{
    public class RevitGenericModelExtractor : IQuantityExtractor
    {
        private readonly Func<Document?> _doc;
        public RevitGenericModelExtractor(Func<Document?> doc)
        {
            _doc = doc;
        }

        public bool CanExtract(long elementId)
        {
            var doc = _doc();
            if (doc == null) return false;

            var elem = doc.GetElement(new ElementId(elementId));
            if (elem is not FamilyInstance fi) return false;
            if (fi.Category.Id.Value != (long)BuiltInCategory.OST_GenericModel) return false;

            var placementType = fi.Symbol.Family.FamilyPlacementType;

            return placementType switch
            {
                FamilyPlacementType.CurveBased => false,
                FamilyPlacementType.CurveBasedDetail => false,
                FamilyPlacementType.TwoLevelsBased => false,
                FamilyPlacementType.ViewBased => false,
                FamilyPlacementType.Adaptive => false,
                FamilyPlacementType.Invalid => false,
                FamilyPlacementType.CurveDrivenStructural => false,
                FamilyPlacementType.OneLevelBasedHosted => (fi.get_Parameter(BuiltInParameter.HOST_VOLUME_COMPUTED)?.AsDouble() ?? 0) > 0,
                FamilyPlacementType.OneLevelBased => (fi.get_Parameter(BuiltInParameter.HOST_VOLUME_COMPUTED)?.AsDouble() ?? 0) > 0,
            };
        }

        public IEnumerable<long> CollectElementIds()
        {
            var doc = _doc();
            if (doc == null) return Enumerable.Empty<long>();

            return new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_GenericModel)
            .WhereElementIsNotElementType()
            .Select(r => r.Id.Value);
        }

        public IEnumerable<QuantityItem> Extract(long elementId)
        {
            var doc = _doc();
            if (doc == null)
                return Enumerable.Empty<QuantityItem>();

            var generic = doc.GetElement(new ElementId(elementId)) as FamilyInstance;
            var quantityItems = new List<QuantityItem>();

            double concValue = UC.Ft3ToM3(RevitGeometryHelper.GetSolids(generic).Sum(s => s.Volume));
            
            var varDict = new Dictionary<string, double>
            {
                ["V"] = concValue,
                ["N"] = 1,
            };

            string materialName = string.Empty;
            var materialId = generic.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM)?.AsElementId() ??
                             generic.Document.GetElement(generic.GetTypeId()).get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM)?.AsElementId();

            if (materialId == null || materialId == ElementId.InvalidElementId)
                materialName = string.Empty;
            else
                materialName = (doc.GetElement(materialId) as Material).Name;

            string typeName = generic.Document.GetElement(generic.GetTypeId()).Name;

            var placementType = generic.Symbol.Family.FamilyPlacementType;

            // 개수 산출
            const string numFormula = "N";
            string numRendered = FormulaCalculator.Render(numFormula, varDict);

            var numItem = new QuantityItem
            {
                ElementId = elementId,
                Category = generic.LookupParameter("DH_Category")?.AsString() ?? string.Empty,
                ElementCode = generic.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                RawFormula = numFormula,
                RenderedFormula = numRendered,
                WorkType = typeName,
                Specification = materialName,
                Value = 1,
                Unit = "EA"
            };
            if (concValue > 1e-6)  quantityItems.Add(numItem);

            // 철근콘크리트
            const string concFormula = "V";
            string concRendered = FormulaCalculator.Render(concFormula, varDict);

            var concreteItem = new QuantityItem
            {
                ElementId = elementId,
                Category = generic.Category.Name ?? string.Empty,
                ElementCode = generic.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                WorkType = "철근콘크리트",
                Specification = materialName,
                RawFormula = concFormula,
                RenderedFormula = concRendered,
                Value = concValue,
                Unit = "m³"
            };

            if (concreteItem.Value > 1e-6) quantityItems.Add(concreteItem);

            return quantityItems;
        }
    }
}
