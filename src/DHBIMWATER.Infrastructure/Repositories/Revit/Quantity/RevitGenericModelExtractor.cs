using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
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

            string materialName = string.Empty;
            var materialId = generic.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM)?.AsElementId() ??
                             generic.Document.GetElement(generic.GetTypeId()).get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM)?.AsElementId();

            if (materialId == null || materialId == ElementId.InvalidElementId)
                materialName = string.Empty;
            else
                materialName = (doc.GetElement(materialId) as Material).Name;

            string typeName = generic.Document.GetElement(generic.GetTypeId()).Name;

            var placementType = generic.Symbol.Family.FamilyPlacementType;

            var varDict = new Dictionary<string, double>
            {
            };

            // 개수 산출
            var numItem = new QuantityItem
            {
                ElementId = elementId,
                Category = generic.LookupParameter("DH_Category")?.AsString() ?? string.Empty,
                ElementCode = generic.LookupParameter("DH_ElementCode")?.AsString() ?? string.Empty,
                WorkType = typeName,
                Specification = materialName,
                Value = 1,
                Unit = "EA"
            };

            quantityItems.Add(numItem);

            return quantityItems;
        }
    }
}
