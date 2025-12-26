using Autodesk.Revit.UI;
using DHBIMWATER.Revit.Commands;
using System.Reflection;

namespace DHBIMWATER.Revit.UI.Modules
{
    internal class QuantityRibbonModule : IRibbonModule
    {
        public void Build(UIControlledApplication app, string ribbonTabName)
        {
            RibbonPanel panel = app.CreateRibbonPanel(ribbonTabName, "Quantity");

            // 버튼이름, 리본에 표시될 텍스트, 어셈블리 경로, 실행될 커맨드 클래스 풀네임
            PushButtonData btn1 = new PushButtonData("QuantityCommand1", "Quantity1", Assembly.GetExecutingAssembly().Location, RevitCommandType<ModelingCommand1>.FullName);
            PushButtonData btn2 = new PushButtonData("QuantityCommand2", "Quantity2", Assembly.GetExecutingAssembly().Location, RevitCommandType<ModelingCommand1>.FullName);
            PushButtonData btn3 = new PushButtonData("QuantityCommand3", "Quantity3", Assembly.GetExecutingAssembly().Location, RevitCommandType<ModelingCommand1>.FullName);
            PushButtonData btn4 = new PushButtonData("QuantityCommand4", "Quantity4", Assembly.GetExecutingAssembly().Location, RevitCommandType<ModelingCommand1>.FullName);

            btn1.LargeImage = RibbonButtonImages.GetIcon("modeling.png");
            btn2.LargeImage = RibbonButtonImages.GetIcon("modeling.png");
            btn3.LargeImage = RibbonButtonImages.GetIcon("modeling.png");
            btn4.LargeImage = RibbonButtonImages.GetIcon("modeling.png");

            panel.AddItem(btn1);
            panel.AddItem(btn2);
            panel.AddItem(btn3);
            panel.AddItem(btn4);
        }
    }
}
