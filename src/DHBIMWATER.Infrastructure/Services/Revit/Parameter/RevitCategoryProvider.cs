using Autodesk.Revit.DB;
using DHBIMWATER.Application.DTOs.Revit;
using DHBIMWATER.Application.Interfaces;

namespace DHBIMWATER.Infrastructure.Services.Revit.Parameter
{
    public class RevitCategoryProvider 
    {
        public IReadOnlyList<CategoryInfo> GetCategories(Document doc)
        {
            var list = new List<CategoryInfo>();
            var map = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var enumType = typeof(BuiltInCategory);
            var underlying = Enum.GetUnderlyingType(enumType);

            var elems = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .ToElements();

            foreach (var elem in elems)
            {
                var cat = elem.Category;
                if (cat == null) continue;

                object keyObj;
                try { keyObj = Convert.ChangeType(cat.Id.Value, underlying); }
                catch { continue; }

                if (!Enum.IsDefined(enumType, keyObj)) continue;
                var bic = (BuiltInCategory)Enum.ToObject(enumType, keyObj);

                if (map.Contains(cat.Name)) continue;
                map.Add(cat.Name);

                list.Add(new CategoryInfo
                {
                    Key = bic.ToString(),
                    DisplayName = cat.Name,
                });
            }

            return list;
        }
    }
}
