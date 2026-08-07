using Autodesk.Revit.UI;
using DHBIMWATER.Revit.Commands;
using System.Reflection;

namespace DHBIMWATER.Revit.UI.Modules
{
    internal class QuantityRibbonModule : IRibbonModule
    {
        public IEnumerable<RibbonItem> Build(UIControlledApplication app, string ribbonTabName)
        {
            RibbonPanel panel = app.CreateRibbonPanel(ribbonTabName, "Quantity");
            string assemblyPath = Assembly.GetExecutingAssembly().Location;

            PushButtonData quantitySettingsBtn = new PushButtonData(
                "QuantitySettingCommand", "수량산출\n설정", assemblyPath, RevitCommandType<QuantitySettingCommand>.FullName);
            quantitySettingsBtn.LargeImage = RibbonButtonImages.GetIcon("Quantity.png");

            PushButtonData quantityBtn = new PushButtonData(
                "QuantityCommand", "수량산출", assemblyPath, RevitCommandType<QuantityCommand>.FullName);
            quantityBtn.LargeImage = RibbonButtonImages.GetIcon("Quantity.png");
            
            return [panel.AddItem(quantitySettingsBtn), panel.AddItem(quantityBtn)];
        }
    }
}
