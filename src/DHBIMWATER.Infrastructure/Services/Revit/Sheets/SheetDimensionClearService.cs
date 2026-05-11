using Autodesk.Revit.DB;

namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class SheetDimensionClearService
    {
        private readonly Document _doc;

        public SheetDimensionClearService(Document doc)
        {
            _doc = doc;
        }

        public void Clear(string sheetId)
        {
            if (!long.TryParse(sheetId, out var sid))
                return;

            var sheet = _doc.GetElement(new ElementId(sid)) as ViewSheet;
            if (sheet == null)
                return;

            using (var tx = new Transaction(_doc, "Clear Sheet Dimensions"))
            {
                tx.Start();

                foreach (var vpId in sheet.GetAllViewports())
                {
                    var vp = _doc.GetElement(vpId) as Viewport;
                    if (vp == null) continue;

                    var view = _doc.GetElement(vp.ViewId) as View;
                    if (view == null) continue;

                    var dimIds = new FilteredElementCollector(_doc, view.Id)
                        .OfClass(typeof(Dimension))
                        .ToElementIds();

                    if (dimIds.Count > 0)
                        _doc.Delete(dimIds);
                }

                tx.Commit();
            }
        }
    }
}
