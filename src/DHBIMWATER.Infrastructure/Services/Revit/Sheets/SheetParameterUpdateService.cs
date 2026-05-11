using Autodesk.Revit.DB;

namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class SheetParameterUpdateService
    {
        private readonly Document _doc;

        public SheetParameterUpdateService(Document doc)
        {
            _doc = doc;
        }

        public void Update(string sheetId, string drawingTitle, string drawingMember, string drawingScale, string drawingNumber)
        {
            if (!long.TryParse(sheetId, out var sid)) return;

            var sheet = _doc.GetElement(new ElementId(sid)) as ViewSheet;
            if (sheet == null) return;

            using (var tx = new Transaction(_doc, "Update Sheet Parameters"))
            {
                tx.Start();

                SetIfExists(sheet, "01.도면 제목", drawingTitle);
                SetIfExists(sheet, "02.도면 부재", drawingMember);
                SetIfExists(sheet, "03.도면 축척", drawingScale);
                SetIfExists(sheet, "04.도면 번호", drawingNumber);

                var titleBlocks = new FilteredElementCollector(_doc, sheet.Id)
                    .OfCategory(BuiltInCategory.OST_TitleBlocks)
                    .WhereElementIsNotElementType()
                    .ToElements();

                foreach (var tb in titleBlocks)
                {
                    SetIfExists(tb, "01. 도면 제목", drawingTitle);
                    SetIfExists(tb, "02. 도면 부재", drawingMember);
                    SetIfExists(tb, "03. 도면 축척", drawingScale);
                    SetIfExists(tb, "04. 도면 번호", drawingNumber);
                }
                tx.Commit();
            }
        }
        private static void SetIfExists(Element element, string paramName, string value)
        {
            var matches = element.GetParameters(paramName);
            if (matches == null || matches.Count == 0) return;

            var p = matches.FirstOrDefault(x => !x.IsReadOnly);
            if (p == null) return;

            value ??= string.Empty;

            if (p.StorageType == StorageType.String)
            {
                p.Set(value);
                return;
            }

            if (p.StorageType == StorageType.Integer)
            {
                if (int.TryParse(value.Replace("1:", ""), out var intValue))
                    p.Set(intValue);
                return;
            }

            if (p.StorageType == StorageType.Double)
            {
                if (double.TryParse(value.Replace("1:", ""), out var doubleValue))
                    p.Set(doubleValue);
            }
        }
    }
}
