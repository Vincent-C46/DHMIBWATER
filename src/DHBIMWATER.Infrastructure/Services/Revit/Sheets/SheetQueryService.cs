using System.Collections.Generic;
using Autodesk.Revit.DB;
using DHBIMWATER.Application.DTOs.Revit;
using DHBIMWATER.Application.DTOs.Revit.Sheet;

namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class SheetQueryService
    {
        private readonly Document _doc;
        private readonly SheetDirectionStorageService _directionStorage;

        public SheetQueryService(Document doc)
        {
            _doc = doc;
            _directionStorage = new SheetDirectionStorageService(doc);
        }

        public IList<SheetInfoDto> GetSheets()
        {
            var list = new List<SheetInfoDto>();

            var sheets = new FilteredElementCollector(_doc)
                            .OfClass(typeof(ViewSheet))
                            .Cast<ViewSheet>()
                            .Where(v => !v.IsTemplate);

            foreach (var s in sheets)
            {
                var dto = new SheetInfoDto
                {
                    Id = s.Id.Value.ToString(),
                    SheetNumber = s.SheetNumber,
                    SheetName = s.Name,
                    ViewDirName = _directionStorage.Load(s) ?? ""
                };

                // ✅ Viewport에 배치된 뷰 추가
                var viewports = new FilteredElementCollector(_doc, s.Id)
                    .OfClass(typeof(Viewport))
                    .Cast<Viewport>();

                foreach (var vp in viewports)
                {
                    var view = _doc.GetElement(vp.ViewId) as View;
                    if (view == null) continue;

                    var viewType = view.ViewType.ToString();
                    if (view.ViewType == ViewType.ThreeD)
                        viewType = "3D";

                    dto.Views.Add(new SheetViewDto
                    {
                        ViewId = view.Id.Value.ToString(),
                        ViewName = view.Name,
                        ViewType = viewType
                    });
                }
                list.Add(dto);
            }
            return list;
        }
    }
}
