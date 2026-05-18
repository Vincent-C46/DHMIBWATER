using System;
using System.Linq;
using Autodesk.Revit.DB;

namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class ViewCategoryService
    {
        private readonly Document _doc;

        public ViewCategoryService(Document doc)
        {
            _doc = doc;
        }

        public void Update(string viewId, string category)
        {
            if (!long.TryParse(viewId, out var vid))
                return;

            var view = _doc.GetElement(new ElementId(vid)) as View;
            if (view == null)
                return;

            using var tx = new Transaction(_doc, "Update View Category");
            tx.Start();
            SetViewCategory(view, category);
            tx.Commit();
        }

        public static void SetViewCategory(View view, string value)
        {
            var candidates = view.Parameters
                .Cast<Autodesk.Revit.DB.Parameter>()
                .Where(IsViewCategoryParameter);

            foreach (var p in candidates)
            {
                if (p == null || p.IsReadOnly || p.StorageType != StorageType.String)
                    continue;

                p.Set(value);
            }
        }

        public static bool HasViewCategory(View view, string expected)
        {
            return view.Parameters
                .Cast<Autodesk.Revit.DB.Parameter>()
                .Where(IsViewCategoryParameter)
                .Any(p =>
                    p != null &&
                    p.StorageType == StorageType.String &&
                    string.Equals(p.AsString(), expected, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsViewCategoryParameter(Autodesk.Revit.DB.Parameter parameter)
        {
            var name = parameter.Definition?.Name;
            if (string.IsNullOrWhiteSpace(name))
                return false;

            var normalized = Normalize(name);
            return normalized == "뷰카테고리" ||
                   normalized == "viewcategory" ||
                   normalized == "viewclassification" ||
                   normalized == "뷰분류" ||
                   normalized.Contains("카테고리") ||
                   normalized.Contains("category");
        }

        private static string Normalize(string value)
        {
            return new string(value
                .Where(c => !char.IsWhiteSpace(c) && c != '_' && c != '-')
                .ToArray())
                .ToLowerInvariant();
        }
    }
}
