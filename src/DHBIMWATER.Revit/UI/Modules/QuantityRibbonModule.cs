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

            PushButtonData quantitySettingsBtn = new PushButtonData("QuantitySettingCommand", "수량산출 설정", Assembly.GetExecutingAssembly().Location, RevitCommandType<QuantitySettingCommand>.FullName);
            quantitySettingsBtn.LargeImage = RibbonButtonImages.GetIcon("Quantity.png");

            PushButtonData quantityBtn = new PushButtonData("QuantityCommand", "수량산출", Assembly.GetExecutingAssembly().Location, RevitCommandType<QuantityCommand>.FullName);
            quantityBtn.LargeImage = RibbonButtonImages.GetIcon("Quantity.png");

            return [panel.AddItem(quantitySettingsBtn), panel.AddItem(quantityBtn)];
        }
    }
}
