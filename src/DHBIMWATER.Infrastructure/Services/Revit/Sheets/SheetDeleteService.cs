using Autodesk.Revit.DB;

namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class SheetDeleteService
    {
        private readonly Document _doc;

        public SheetDeleteService(Document doc)
        {
            _doc = doc;
        }

        public void DeleteSheet(string sheetId)
        {
            var id = new ElementId(long.Parse(sheetId));
            using (var tx = new Transaction(_doc, "Delete Sheet"))
            {
                tx.Start();
                _doc.Delete(id);
                tx.Commit();
            }
        }

    }
}
