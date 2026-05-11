using System.Linq;
using Autodesk.Revit.DB;

namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class ViewportMoveService
    {
        private readonly Document _doc;

        public ViewportMoveService(Document doc)
        {
            _doc = doc;
        }

        public void Move(string sheetId, string viewId, double x, double y)
        {
            if (!long.TryParse(sheetId, out var sid)) return;
            if (!long.TryParse(viewId, out var vid)) return;

            var sId = new ElementId(sid);
            var vId = new ElementId(vid);

            var viewport = new FilteredElementCollector(_doc, sId)
                .OfClass(typeof(Viewport))
                .Cast<Viewport>()
                .FirstOrDefault(vp => vp.ViewId == vId);

            if (viewport == null) return;

            using (var tx = new Transaction(_doc, "Move Viewport To Point"))
            {
                tx.Start();
                viewport.SetBoxCenter(new XYZ(x, y, 0));
                tx.Commit();
            }
        }
        public void MoveBySheetRatio(string sheetId, string viewId, double uRatio, double vRatio)
        {
            if (!long.TryParse(sheetId, out var sid)) return;
            if (!long.TryParse(viewId, out var vid)) return;

            var sId = new ElementId(sid);
            var vId = new ElementId(vid);

            var sheet = _doc.GetElement(sId) as ViewSheet;
            if (sheet == null) return;

            var viewport = new FilteredElementCollector(_doc, sId)
                .OfClass(typeof(Viewport))
                .Cast<Viewport>()
                .FirstOrDefault(vp => vp.ViewId == vId);

            if (viewport == null) return;

            var outline = sheet.Outline;
            var x = outline.Min.U + (outline.Max.U - outline.Min.U) * uRatio;
            var y = outline.Min.V + (outline.Max.V - outline.Min.V) * vRatio;

            using (var tx = new Transaction(_doc, "Move Viewport By Ratio"))
            {
                tx.Start();
                viewport.SetBoxCenter(new XYZ(x, y, 0));
                tx.Commit();
            }
        }
        public void UpdateTitleLayout(string sheetId, string viewId, double offsetX, double offsetY, double lineLength)
        {
            if (!long.TryParse(sheetId, out var sid)) return;
            if (!long.TryParse(viewId, out var vid)) return;

            var sId = new ElementId(sid);
            var vId = new ElementId(vid);

            var viewport = new FilteredElementCollector(_doc, sId)
                .OfClass(typeof(Viewport))
                .Cast<Viewport>()
                .FirstOrDefault(vp => vp.ViewId == vId);

            if (viewport == null) return;

            using (var tx = new Transaction(_doc, "Update Viewport Title Layout"))
            {
                tx.Start();

                viewport.LabelOffset = new XYZ(offsetX, offsetY, 0);
                viewport.LabelLineLength = lineLength;

                tx.Commit();
            }
        }

    }
}
