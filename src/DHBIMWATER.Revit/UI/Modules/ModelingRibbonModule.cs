using Autodesk.Revit.UI;
using DHBIMWATER.Revit.Commands;
using System.Reflection;

namespace DHBIMWATER.Revit.UI.Modules
{
    internal class ModelingRibbonModule : IRibbonModule
    {
        public void Build(UIControlledApplication app, string ribbonTabName)
        {
            RibbonPanel panel = app.CreateRibbonPanel(ribbonTabName, "Modeling");

            // 버튼이름, 리본에 표시될 텍스트, 어셈블리 경로, 실행될 커맨드 클래스 풀네임
            PushButtonData btn1 = new PushButtonData("ModelingCommand1", "Modeling1", Assembly.GetExecutingAssembly().Location, RevitCommandType<ModelingCommand1>.FullName);
            PushButtonData btnGuideLine = new PushButtonData("GuideLineCommand", "GuideLine", Assembly.GetExecutingAssembly().Location, RevitCommandType<GuideLineCommand>.FullName);
            PushButtonData btn3 = new PushButtonData("ModelingCommand3", "Modeling3", Assembly.GetExecutingAssembly().Location, RevitCommandType<ModelingCommand1>.FullName);
            PushButtonData btn4 = new PushButtonData("ModelingCommand4", "Modeling4", Assembly.GetExecutingAssembly().Location, RevitCommandType<ModelingCommand1>.FullName);

            btn1.LargeImage = RibbonButtonImages.GetIcon("modeling.png");
            btnGuideLine.LargeImage = RibbonButtonImages.GetIcon("revit.png");
            btn3.LargeImage = RibbonButtonImages.GetIcon("modeling.png");
            btn4.LargeImage = RibbonButtonImages.GetIcon("modeling.png");

            panel.AddItem(btn1);
            panel.AddItem(btnGuideLine);
            panel.AddItem(btn3);
            panel.AddItem(btn4);
        }
    }
}
