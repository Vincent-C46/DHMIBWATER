using Autodesk.Revit.DB;
using DHBIMWATER.Application.DTOs.Revit.Sheet;

namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class SheetCreateService
    {
        private readonly Document _doc;

        public SheetCreateService(Document doc)
        {
            _doc = doc;
        }

        public SheetInfoDto CreateSheet(string titleBlockId, string sheetNumber, string sheetName)
        {
            var tbId = new ElementId(long.Parse(titleBlockId));

            ViewSheet sheet;
            using (var tx = new Transaction(_doc, "Create Sheet"))
            {
                tx.Start();
                sheet = ViewSheet.Create(_doc, tbId);
                sheet.SheetNumber = sheetNumber;
                sheet.Name = sheetName;
                tx.Commit();
            }

            return new SheetInfoDto
            {
                Id = sheet.Id.Value.ToString(),
                SheetNumber = sheet.SheetNumber,
                SheetName = sheet.Name
            };
        }
    }
}
