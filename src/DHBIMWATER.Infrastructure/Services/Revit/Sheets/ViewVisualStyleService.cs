using Autodesk.Revit.DB;

namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class ViewVisualStyleService
    {
        private readonly Document _doc;
        public ViewVisualStyleService(Document doc) { _doc = doc; }

        public void UpdateViewVisualStyle(string viewId, string visualStyle)
        {
            if (!long.TryParse(viewId, out var id)) return;

            var view = _doc.GetElement(new ElementId(id)) as View;
            if (view == null) return;

            var style = ParseStyle(visualStyle);

            using (var tx = new Transaction(_doc, "Update View Visual Style"))
            {
                tx.Start();
                view.DisplayStyle = style;
                tx.Commit();
            }
        }

        private static DisplayStyle ParseStyle(string text)
        {
            return text switch
            {
                "와이어프레임" => DisplayStyle.Wireframe,
                "은선" => DisplayStyle.HLR,
                "음영처리" => DisplayStyle.Shading,
                "일관된 색상" => DisplayStyle.FlatColors,
                "텍스처" => DisplayStyle.RealisticWithEdges,
                "사실적" => DisplayStyle.Realistic,
                _ => DisplayStyle.HLR
            };
        }
    }
}
