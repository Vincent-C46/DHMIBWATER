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
        public IEnumerable<RibbonItem> Build(UIControlledApplication app, string ribbonTabName)
        {
            // //기존 페널 작성 //
            //RibbonPanel panel = app.CreateRibbonPanel(ribbonTabName, "Utility");

            //// 대상 텝에 페널 작성 ////
            string panelName = "Utility";
            RibbonPanel panel = RibbonUiHelper.GetOrCreateRibbonPanel(app, ribbonTabName, panelName);


            // 버튼이름, 리본에 표시될 텍스트, 어셈블리 경로, 실행될 커맨드 클래스 풀네임-
            PushButtonData btn1 = new PushButtonData("ExParamsCommand", "Export", Assembly.GetExecutingAssembly().Location, RevitCommandType<ExParamsCommand>.FullName);
            // DHBoost.Revit.dll 내부의 기준점 통합 링크 커맨드를 그대로 실행 (코드 이관 아님)
            //PushButtonData btn2 = new PushButtonData("UtilityCommand2", "Utility2", Assembly.GetExecutingAssembly().Location, RevitCommandType<ModelingCommand1>.FullName);
            //PushButtonData btn3 = new PushButtonData("UtilityCommand3", "Utility3", Assembly.GetExecutingAssembly().Location, RevitCommandType<ModelingCommand1>.FullName);
            //PushButtonData btn4 = new PushButtonData("UtilityCommand4", "Utility4", Assembly.GetExecutingAssembly().Location, RevitCommandType<ModelingCommand1>.FullName);

            btn1.LargeImage = RibbonButtonImages.GetIcon("Export.png");
            // TODO: 기준점 통합 링크 전용 아이콘이 준비되면 modeling.png 대신 교체
            //btn2.LargeImage = RibbonButtonImages.GetIcon("modeling.png");
            //btn3.LargeImage = RibbonButtonImages.GetIcon("modeling.png");
            //btn4.LargeImage = RibbonButtonImages.GetIcon("modeling.png");

     
            //panel.AddItem(btn2);
            //panel.AddItem(btn3);
            //panel.AddItem(btn4);  

            return [
                panel.AddItem(btn1),
                ];
        }       
    }
}
