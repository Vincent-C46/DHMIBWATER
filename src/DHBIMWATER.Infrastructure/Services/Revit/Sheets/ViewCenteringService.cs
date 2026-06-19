using System.Linq;
using Autodesk.Revit.DB;

namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class ViewCenteringService
    {
        private readonly Document _doc;
        public ViewCenteringService(Document doc) { _doc = doc; }

        public void RecenterViewportToSheetCenter(string sheetId, string viewId)
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
            var center  = new XYZ(
                (outline.Min.U + outline.Max.U) * 0.5,
                (outline.Min.V + outline.Max.V) * 0.5,
                0);

            using var tx = new Transaction(_doc, "Recenter Viewport");
            tx.Start();
            viewport.SetBoxCenter(center);
            tx.Commit();
        }
    }
}
