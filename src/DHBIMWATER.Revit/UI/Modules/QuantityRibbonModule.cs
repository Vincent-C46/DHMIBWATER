using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using DHBIMWATER.Revit.Commands;
using DHBIMWATER.Revit.Commands.Quantity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Revit.UI.Modules
{
    internal class QuantityRibbonModule : IRibbonModule
    {
        public void Build(UIControlledApplication app, string ribbonTabName)
        {
            RibbonPanel panel = app.CreateRibbonPanel(ribbonTabName, "Quantity");

            PushButtonData quantityBtn = new PushButtonData("QuantityCommand", "수량산출", Assembly.GetExecutingAssembly().Location, RevitCommandType<QuantityCommand>.FullName);
            quantityBtn.LargeImage = RibbonButtonImages.GetIcon("Quantity.png");
            panel.AddItem(quantityBtn);
        }
    }
}
