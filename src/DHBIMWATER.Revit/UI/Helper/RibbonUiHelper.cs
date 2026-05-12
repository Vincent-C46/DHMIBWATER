using System.Linq;
using Autodesk.Revit.UI;

namespace DHBIMWATER.Revit.UI.Helper
{
    internal static class RibbonUiHelper
    {
        internal static void EnsureRibbonTab(UIControlledApplication app, string tabName)
        {
            try
            {
                app.CreateRibbonTab(tabName);
            }
            catch
            {
                // already exists
            }
        }

        internal static RibbonPanel GetOrCreateRibbonPanel(
            UIControlledApplication app,
            string tabName,
            string panelName)
        {
            RibbonPanel existingPanel = app
                .GetRibbonPanels(tabName)
                .FirstOrDefault(p => p.Name == panelName);

            if (existingPanel != null)
                return existingPanel;

            return app.CreateRibbonPanel(tabName, panelName);
        }
    }
}
