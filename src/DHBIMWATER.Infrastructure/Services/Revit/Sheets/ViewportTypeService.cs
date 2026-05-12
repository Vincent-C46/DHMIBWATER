using System.Linq;
using Autodesk.Revit.DB;

namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class ViewportTypeService
    {
        private readonly Document _doc;

        public ViewportTypeService(Document doc)
        {
            _doc = doc;
        }

        public void SetViewportType(string sheetId, string viewId, string viewportTypeName)
        {
            if (!long.TryParse(sheetId, out var sid)) return;
            if (!long.TryParse(viewId, out var vid)) return;
            if (string.IsNullOrWhiteSpace(viewportTypeName)) return;

            var sId = new ElementId(sid);
            var vId = new ElementId(vid);

            var viewport = new FilteredElementCollector(_doc, sId)
                .OfClass(typeof(Viewport))
                .Cast<Viewport>()
                .FirstOrDefault(vp => vp.ViewId == vId);

            if (viewport == null) return;

            var validTypeIds = viewport.GetValidTypes();

            var viewportType = validTypeIds
                .Select(id => _doc.GetElement(id) as ElementType)
                .FirstOrDefault(x =>
                    x != null &&
                    x.Name.Equals(viewportTypeName, StringComparison.OrdinalIgnoreCase));


            if (viewportType == null) return;

            using (var tx = new Transaction(_doc, "Set Viewport Type"))
            {
                tx.Start();
                viewport.ChangeTypeId(viewportType.Id);
                tx.Commit();
            }
        }
    }
}
