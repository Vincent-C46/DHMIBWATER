using Autodesk.Revit.UI;
using DHBIMWATER.Revit.Commands;
using System.Reflection;

namespace DHBIMWATER.Revit.UI.Modules
{
    /// <summary>
    /// 수량산출 리본 패널.
    /// DHBIMWATER 프록시 커맨드를 통해 Costura에 내장된 DHBoost 수량산출 커맨드를 실행한다.
    /// </summary>
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
