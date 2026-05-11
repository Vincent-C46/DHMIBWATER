using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class ViewActivationService
    {
        private readonly UIDocument _uidoc;
        private readonly Document _doc;

        public ViewActivationService(UIDocument uidoc)
        {
            _uidoc = uidoc;
            _doc = uidoc.Document;
        }

        public string GetActiveViewId()
        {
            return _uidoc.ActiveView?.Id.Value.ToString();
        }

        public void ActivateView(string viewId)
        {
            if (!long.TryParse(viewId, out var vid))
                return;

            var view = _doc.GetElement(new ElementId(vid)) as View;
            if (view == null)
                return;

            _uidoc.ActiveView = view;
        }
    }
}
