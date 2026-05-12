using System.Linq;
using Autodesk.Revit.DB;

namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class ViewReplaceService
    {
        private readonly Document _doc;
        public ViewReplaceService(Document doc)
        {
            _doc = doc;
        }

        public void ReplaceViewOnSheet(string sheetId, string oldViewId, string newViewId)
        {
            var sId = new ElementId(long.Parse(sheetId));
            var oldId = new ElementId(long.Parse(oldViewId));
            var newId = new ElementId(long.Parse(newViewId));


            var sheet = _doc.GetElement(sId) as ViewSheet;
            if (sheet == null) return;

            var viewport = new FilteredElementCollector(_doc, sId)
                .OfClass(typeof(Viewport))
                .Cast<Viewport>()
                .FirstOrDefault(vp => vp.ViewId == oldId);

            if (viewport == null) return;

            var center = viewport.GetBoxCenter();

            using (var tx = new Transaction(_doc, "Replace View On Sheet"))
            {
                tx.Start();
                _doc.Delete(viewport.Id);

                if (Viewport.CanAddViewToSheet(_doc, sId, newId))
                    Viewport.Create(_doc, sId, newId, center);

                tx.Commit();
            }
        }
    }
}
