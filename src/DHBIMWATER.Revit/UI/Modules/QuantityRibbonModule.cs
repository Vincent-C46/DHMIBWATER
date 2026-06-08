using Autodesk.Revit.UI;
using DHBIMWATER.Revit.Commands;
using DHBIMWATER.Revit.Commands.Quantity;
using System.Reflection;

namespace DHBIMWATER.Revit.UI.Modules
{
    internal class QuantityRibbonModule : IRibbonModule
    {
        public IEnumerable<RibbonItem> Build(UIControlledApplication app, string ribbonTabName)
        {
            RibbonPanel panel = app.CreateRibbonPanel(ribbonTabName, "Quantity");
            PushButtonData quantityBtn = new PushButtonData("QuantityCommand", "수량산출", Assembly.GetExecutingAssembly().Location, RevitCommandType<QuantityCommand>.FullName);
            quantityBtn.LargeImage = RibbonButtonImages.GetIcon("Quantity.png");

            return [panel.AddItem(quantityBtn)];
        }
    }
}
