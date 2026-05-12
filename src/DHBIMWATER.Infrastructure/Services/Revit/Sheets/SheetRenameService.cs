using Autodesk.Revit.DB;

namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class SheetRenameService
    {
        private readonly Document _doc;

        public SheetRenameService(Document doc)
        {
            _doc = doc;
        }

        public void RenameSheet(string sheetId, string newName)
        {
            var id = new ElementId(long.Parse(sheetId));
            var sheet = _doc.GetElement(id) as ViewSheet;
            if (sheet == null) return;

            using (var tx = new Transaction(_doc, "Rename Sheet"))
            {
                tx.Start();
                sheet.Name = newName;
                tx.Commit();
            }
        }

    }
}
