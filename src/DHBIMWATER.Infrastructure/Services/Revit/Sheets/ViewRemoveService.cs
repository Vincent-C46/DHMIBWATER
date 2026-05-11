using System.Linq;
using Autodesk.Revit.DB;

namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class ViewRemoveService
    {
        private readonly Document _doc;

        public ViewRemoveService(Document doc)
        {
            _doc = doc;
        }

        public void RemoveView(string sheetId, string viewId)
        {
            var sId = new ElementId(long.Parse(sheetId));
            var vId = new ElementId(long.Parse(viewId));

            var viewport = new FilteredElementCollector(_doc, sId)
                .OfClass(typeof(Viewport))
                .Cast<Viewport>()
                .FirstOrDefault(vp => vp.ViewId == vId);

            if (viewport == null) return;

            using (var tx = new Transaction(_doc, "Remove View From Sheet"))
            {
                tx.Start();
                _doc.Delete(viewport.Id);
                tx.Commit();
            }
        }
    }
}
