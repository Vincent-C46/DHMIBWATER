using Autodesk.Revit.DB;
using DHBIMWATER.Application.Interfaces;

namespace DHBIMWATER.Infrastructure.Services.Revit.Parameter
{
    public class RevitCategoryParameterProvider 
    {
        public IReadOnlyList<string> GetParameters(Document doc, BuiltInCategory category)
        {
            var elems = new FilteredElementCollector(doc)
                .OfCategory(category)
                .WhereElementIsNotElementType()
                .ToElements();

            return elems
                .SelectMany(e => e.Parameters.Cast<Autodesk.Revit.DB.Parameter>())
                .Where(p => p?.Definition != null)
                .Select(p => p.Definition.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n)
                .ToList();
        }
    }
}
    