using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using DHBIMWATER.Application.DTOs.Revit.Sheet;
using Autodesk.Revit.DB.ExtensibleStorage;

namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class ViewQueryService
    {
        private readonly Document _doc;
        public ViewQueryService(Document doc) { _doc = doc; }
        private static readonly Guid FormSchemaGuid = new("3F9D1A7D-8A9A-4D9C-9B7B-5A6D31D4F211");
        private const string FieldName = "SheetForm";

        public IList<ViewInfoDto> GetViews()
        {            
            var views = new FilteredElementCollector(_doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v =>
                    !v.IsTemplate &&
                    v.ViewType != ViewType.DrawingSheet &&
                    v.ViewType != ViewType.Schedule &&
                    v.ViewType != ViewType.DraftingView &&
                    v.ViewType != ViewType.ProjectBrowser &&
                    v.ViewType != ViewType.SystemBrowser &&
                    v.ViewType != ViewType.Report
                    );

            return views.Select(v => new ViewInfoDto
            {
                ViewId = v.Id.Value.ToString(),
                ViewName = v.Name,
                ViewType = v.ViewType == ViewType.ThreeD ? "3D" : v.ViewType.ToString(),
                Scale = v.Scale,
                ScaleText = $"1:{v.Scale}",
                VisualStyle = ToVisualStyleText(v.DisplayStyle),
                TitleOnSheet = GetTitleOnSheet(v),
                SheetForm = GetSavedForm(v)
            }).ToList();
        }
        private static string ToVisualStyleText(DisplayStyle s)
        {
            return s switch
            {
                DisplayStyle.Wireframe => "와이어프레임",
                DisplayStyle.HLR => "은선",
                DisplayStyle.Shading => "음영처리",
                DisplayStyle.FlatColors => "일관된 색상",
                DisplayStyle.RealisticWithEdges => "텍스처",
                DisplayStyle.Realistic => "사실적",
                _ => "은선"
            };
        }
        private static string GetTitleOnSheet(View view)
        {
            var p = view.get_Parameter(BuiltInParameter.VIEW_DESCRIPTION);
            if (p != null && p.HasValue) return p.AsString() ?? "";

            p = view.LookupParameter("시트의 제목");
            if (p != null && p.HasValue) return p.AsString() ?? "";

            p = view.LookupParameter("Title on Sheet");
            if (p != null && p.HasValue) return p.AsString() ?? "";

            return "";
        }
        private static string GetSavedForm(View view)
        {
            var schema = Schema.Lookup(FormSchemaGuid);
            if (schema == null) return "없음";

            var ent = view.GetEntity(schema);
            if (!ent.IsValid()) return "없음";

            var field = schema.GetField(FieldName);
            if (field == null) return "없음";

            var v = ent.Get<string>(field);
            return string.IsNullOrWhiteSpace(v) ? "없음" : v;
        }
    }
}
