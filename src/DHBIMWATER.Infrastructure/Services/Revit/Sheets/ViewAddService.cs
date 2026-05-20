using Autodesk.Revit.DB;

namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class ViewAddService
    {
        private readonly Document _doc;
        private readonly ViewSheetPreparationService _prepare;
        public ViewAddService(Document doc)
        {
            _doc = doc;
            _prepare = new ViewSheetPreparationService(doc);
        }

        public string AddViewToSheet(string sheetId, string viewId, string suffix = "_시트", string targetViewName = null, bool duplicate = true)
        {
            var sId = new ElementId(long.Parse(sheetId));
            var preparedViewId = duplicate
                ? _prepare.CreateSheetView(viewId, suffix, targetViewName)
                : viewId;

            var vId = new ElementId(long.Parse(preparedViewId));


            var sheet = _doc.GetElement(sId) as ViewSheet;
            if (sheet == null) return null;

            // 임시 위치 (원하면 나중에 조정)
            var pt = GetNextViewLocation(sheet);

            using (var tx = new Transaction(_doc, "Add View To Sheet"))
            {
                tx.Start();

                if (!Viewport.CanAddViewToSheet(_doc, sId, vId))
                {
                    tx.RollBack();
                    return null;
                }

                Viewport.Create(_doc, sId, vId, pt);
                tx.Commit();
            }
            return preparedViewId;

        }

        private XYZ GetNextViewLocation(ViewSheet sheet)
        {
            var outline = sheet.Outline; // UV
            var centerU = (outline.Min.U + outline.Max.U) * 0.5;
            var centerV = (outline.Min.V + outline.Max.V) * 0.5;
            return new XYZ(centerU, centerV, 0);
        }
        public void HideSectionMarkersOnReservoirSectionViews()
        {
            _prepare.HideSectionMarkersOnReservoirSectionViews();
        }

        public void HideCopiedSectionMarkersOnReservoirPlanViews()
        {
            _prepare.HideCopiedSectionMarkersOnReservoirPlanViews();
        }

    }
}
