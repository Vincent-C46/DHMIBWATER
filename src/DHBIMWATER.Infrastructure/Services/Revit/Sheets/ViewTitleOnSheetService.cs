using Autodesk.Revit.DB;

namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class ViewTitleOnSheetService
    {
        private readonly Document _doc;
        public ViewTitleOnSheetService(Document doc) { _doc = doc; }

        public void Update(string viewId, string titleOnSheet)
        {
            if (!long.TryParse(viewId, out var vid)) return;
            if (string.IsNullOrWhiteSpace(titleOnSheet)) return;

            var view = _doc.GetElement(new ElementId(vid)) as View;
            if (view == null) return;

            using (var tx = new Transaction(_doc, "Update View Title On Sheet"))
            {
                tx.Start();

                // 우선 BuiltInParameter 사용
                var p = view.get_Parameter(BuiltInParameter.VIEW_DESCRIPTION);

                // 프로젝트/언어별 예외 fallback
                if (p == null) p = view.LookupParameter("시트의 제목");
                if (p == null) p = view.LookupParameter("Title on Sheet");

                if (p != null && !p.IsReadOnly)
                    p.Set(titleOnSheet);

                tx.Commit();
            }
        }
    }
}
