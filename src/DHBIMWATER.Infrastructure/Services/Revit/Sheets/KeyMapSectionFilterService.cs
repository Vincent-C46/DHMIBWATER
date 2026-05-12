using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;

namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class KeyMapSectionFilterService
    {
        private readonly Document _doc;

        public KeyMapSectionFilterService(Document doc)
        {
            _doc = doc;
        }

        public void Apply(string viewId, string sectionName)
        {
            if (!long.TryParse(viewId, out var vid)) return;
            var view = _doc.GetElement(new ElementId(vid)) as View;
            if (view == null) return;

            using var tx = new Transaction(_doc, "Filter KeyMap Sections");
            tx.Start();

            var hideIds = new List<ElementId>();

            var elems = new FilteredElementCollector(_doc, view.Id)
                .WhereElementIsNotElementType()
                .ToElements();

            foreach (var e in elems)
            {
                if (e?.Category == null) continue;
                if (!e.CanBeHidden(view)) continue;

                var bic = (BuiltInCategory)e.Category.Id.Value;
                if (bic != BuiltInCategory.OST_Sections &&
                    bic != BuiltInCategory.OST_SectionHeads &&
                    bic != BuiltInCategory.OST_Viewers &&
                    bic != BuiltInCategory.OST_Views)
                    continue;

                

                var name = e.Name ?? string.Empty;
                var type = _doc.GetElement(e.GetTypeId())?.Name ?? string.Empty;
                var targetName = $"{sectionName}";
                var match =
                    name.Equals(targetName, StringComparison.OrdinalIgnoreCase) ||
                    type.Equals(targetName, StringComparison.OrdinalIgnoreCase);

                if (!match)
                    hideIds.Add(e.Id);
            }

            if (hideIds.Count > 0)
                view.HideElements(hideIds);

            tx.Commit();
        }
    }
}
