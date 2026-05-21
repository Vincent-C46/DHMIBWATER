using Autodesk.Revit.DB;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;
using System.Windows.Controls;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Quantity
{
    public class RevitFoundationExtractor : IQuantityExtractor
    {

        private readonly Func<Document?> _doc;

        public RevitFoundationExtractor(Func<Document?> doc)
        {
            _doc = doc;
        }

        public bool CanExtract(long elementId)
        {
            var doc = _doc();
            if (doc == null) return false;
            var elem = doc.GetElement(new ElementId(elementId));
            return elem is FamilyInstance fi && fi.Category.Id.Value == (int)BuiltInCategory.OST_StructuralFoundation;
        }

        public IEnumerable<long> CollectElementIds()
        {
            var doc = _doc();
            if (doc == null) return Enumerable.Empty<long>();

            return new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_StructuralFoundation)
                        .WhereElementIsNotElementType()
                        .Select(r => r.Id.Value);
        }

        public IEnumerable<QuantityItem> Extract(long elementId)
        {
            throw new NotImplementedException();
        }
    }
}
