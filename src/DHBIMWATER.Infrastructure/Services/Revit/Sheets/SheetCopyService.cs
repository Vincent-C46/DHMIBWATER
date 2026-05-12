using Autodesk.Revit.DB;
using DHBIMWATER.Application.DTOs.Revit.Sheet;

namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class SheetCopyService
    {
        private readonly Document _doc;

        public SheetCopyService(Document doc)
        {
            _doc = doc;
        }

        public SheetInfoDto CopySheet(string sheetId)
        {
            var srcId = new ElementId(long.Parse(sheetId));
            var src = _doc.GetElement(srcId) as ViewSheet;
            if (src == null) return null;

            // titleblock 복사
            var titleBlock = new FilteredElementCollector(_doc, src.Id)
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .FirstElement();

            var tbId = titleBlock?.GetTypeId() ?? ElementId.InvalidElementId;

            ViewSheet newSheet;
            using (var tx = new Transaction(_doc, "Copy Sheet"))
            {
                tx.Start();
                newSheet = ViewSheet.Create(_doc, tbId);
                newSheet.SheetNumber = src.SheetNumber + "_COPY";
                newSheet.Name = src.Name + "_Copy";
                tx.Commit();
            }

            return new SheetInfoDto
            {
                Id = newSheet.Id.Value.ToString(),
                SheetNumber = newSheet.SheetNumber,
                SheetName = newSheet.Name
            };
        }

    }
}
