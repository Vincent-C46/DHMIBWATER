using Autodesk.Revit.DB;

namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class ViewScaleService
    {
        private readonly Document _doc;
        public ViewScaleService(Document doc) { _doc = doc; }

        public void UpdateViewScale(string viewId, int scale)
        {
            if (!long.TryParse(viewId, out var id)) return;
            if (scale <= 0) return;

            var view = _doc.GetElement(new ElementId(id)) as View;
            if (view == null) return;

            using (var tx = new Transaction(_doc, "Update View Scale"))
            {
                tx.Start();
                view.Scale = scale;
                tx.Commit();
            }
        }
    }
}
