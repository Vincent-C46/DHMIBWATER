using System.Linq;
using System.Reflection;
using Autodesk.Revit.UI;
using DHBIMWATER.Revit.Commands;
using DHBIMWATER.Revit.Commands.Parameter;
using DHBIMWATER.Revit.UI.Helper;

namespace DHBIMWATER.Revit.UI.Modules
{
    internal class UtilityRibbonModule : IRibbonModule
    {
        public void Build(UIControlledApplication app, string ribbonTabName)
        {
            //기존 페널 작성 //
           RibbonPanel panel = app.CreateRibbonPanel(ribbonTabName, "Utility");

            ////// 대상 텝에 페널 작성 ////
            //string panelName = "Utility";
            //RibbonPanel panel = RibbonUiHelper.GetOrCreateRibbonPanel(app, ribbonTabName, panelName);


            // 버튼이름, 리본에 표시될 텍스트, 어셈블리 경로, 실행될 커맨드 클래스 풀네임
            PushButtonData btn1 = new PushButtonData("ExParamsCommand", "Export", Assembly.GetExecutingAssembly().Location, RevitCommandType<ExParamsCommand>.FullName);
            //PushButtonData btn2 = new PushButtonData("UtilityCommand2", "Utility2", Assembly.GetExecutingAssembly().Location, RevitCommandType<ModelingCommand1>.FullName);
            //PushButtonData btn3 = new PushButtonData("UtilityCommand3", "Utility3", Assembly.GetExecutingAssembly().Location, RevitCommandType<ModelingCommand1>.FullName);
            //PushButtonData btn4 = new PushButtonData("UtilityCommand4", "Utility4", Assembly.GetExecutingAssembly().Location, RevitCommandType<ModelingCommand1>.FullName);

            btn1.LargeImage = RibbonButtonImages.GetIcon("Export.png");
            //btn2.LargeImage = RibbonButtonImages.GetIcon("modeling.png");
            //btn3.LargeImage = RibbonButtonImages.GetIcon("modeling.png");
            //btn4.LargeImage = RibbonButtonImages.GetIcon("modeling.png");

            panel.AddItem(btn1);
            //panel.AddItem(btn2);
            //panel.AddItem(btn3);
            //panel.AddItem(btn4);
        }       
    }
}
