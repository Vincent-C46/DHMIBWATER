using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class TagService
    {
        private readonly Document _doc;

        public TagService(Document doc)
        {
            _doc = doc;
        }

        public void ApplyTagsToSelectedOnCurrentView(IList<string> elementIds)
        {
            if (elementIds == null || elementIds.Count == 0)
                return;

            var ids = elementIds
                .Select(x => long.TryParse(x, out var v) ? new ElementId(v) : ElementId.InvalidElementId)
                .Where(x => x != ElementId.InvalidElementId)
                .ToList();

            if (ids.Count == 0)
                return;

            var view = _doc.ActiveView;
            if (view == null)
                return;

            ApplyTagsToElementsOnView(view, ids);
        }

        private XYZ GetTagPoint(Element element, View view)
        {
            if (element is Wall wall)
            {
                var lc = wall.Location as LocationCurve;
                if (lc?.Curve == null)
                    return null;

                return lc.Curve.Evaluate(0.5, true);
            }

            var bb = element.get_BoundingBox(view);
            if (bb == null)
                return null;

            return (bb.Min + bb.Max) * 0.5;
        }
        public void ApplyTagsToAllOnCurrentView()
        {
            var view = _doc.ActiveView;
            if (view == null)
                return;

            ApplyTagsToAllOnView(view);
        }
        private void ApplyTagsToAllOnView(View view)
        {
            var ids = new FilteredElementCollector(_doc, view.Id)
                .WhereElementIsNotElementType()
                .Where(e => e.Category != null)
                .Where(e =>
                {
                    var bic = (BuiltInCategory)e.Category.Id.Value;
                    return bic == BuiltInCategory.OST_Walls ||
                           bic == BuiltInCategory.OST_Floors;
                })
                .Select(e => e.Id)
                .ToList();

            ApplyTagsToElementsOnView(view, ids);
        }

        private void ApplyTagsToElementsOnView(View view, IList<ElementId> ids)
        {
            if (ids == null || ids.Count == 0)
                return;           

            var existingTaggedIds = new HashSet<long>();

            var existingTags = new FilteredElementCollector(_doc, view.Id)
                .OfClass(typeof(IndependentTag))
                .Cast<IndependentTag>()
                .ToList();

            foreach (var tag in existingTags)
            {
                try
                {
                    var taggedIds = tag.GetTaggedLocalElementIds();
                    foreach (var taggedId in taggedIds)
                        existingTaggedIds.Add(taggedId.Value);
                }
                catch
                {
                }
            }

            using (var tx = new Transaction(_doc, "Apply Tags On Current View"))
            {
                tx.Start();

                foreach (var id in ids)
                {
                    if (existingTaggedIds.Contains(id.Value))
                        continue;

                    var element = _doc.GetElement(id);
                    if (element == null || element.Category == null)
                        continue;

                    var bic = (BuiltInCategory)element.Category.Id.Value;
                    if (bic != BuiltInCategory.OST_Walls &&
                        bic != BuiltInCategory.OST_Floors)
                        continue;

                    try
                    {
                        var reference = new Reference(element);
                        var point = GetTagPoint(element, view);
                        if (point == null)
                            continue;

                        bool hasLeader = bic == BuiltInCategory.OST_Walls;

                        var tag = IndependentTag.Create(
                            _doc,
                            view.Id,
                            reference,
                            hasLeader,
                            TagMode.TM_ADDBY_CATEGORY,
                            TagOrientation.Horizontal,
                            point);

                        if (tag != null)
                            tag.HasLeader = hasLeader;
                    }
                    catch
                    {
                    }
                }

                tx.Commit();
            }
        }
        public void ApplyReservoirTags(string sheetId)
        {
            if (!long.TryParse(sheetId, out var sid))
                return;

            var sheet = _doc.GetElement(new ElementId(sid)) as ViewSheet;
            if (sheet == null)
                return;

            foreach (var vpId in sheet.GetAllViewports())
            {
                var vp = _doc.GetElement(vpId) as Viewport;
                if (vp == null)
                    continue;

                var view = _doc.GetElement(vp.ViewId) as View;
                if (view == null)
                    continue;

                if (view is not ViewPlan && view is not ViewSection)
                    continue;

                if (view.Name.Contains("KeyMap", StringComparison.OrdinalIgnoreCase) ||
                    view.Name.Contains("KEY PLAN", StringComparison.OrdinalIgnoreCase))
                    continue;

                ApplyTagsToAllOnView(view);
            }
        }
    }
}
