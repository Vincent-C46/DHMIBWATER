using System.Reflection;
using Autodesk.Revit.UI;
using DHBIMWATER.Revit.Commands;
using DHBIMWATER.Revit.Commands.Families;
using DHBIMWATER.Revit.Commands.Sheets;
using DHBIMWATER.Revit.UI.Helper;

namespace DHBIMWATER.Revit.UI.Modules
{
    internal class DocumentationRibbonModule : IRibbonModule
    {
        public void Build(UIControlledApplication app, string ribbonTabName)
        {
            //기존 페널 작성 //
            RibbonPanel panel = app.CreateRibbonPanel(ribbonTabName, "Documentation");

            ////// 대상 텝에 페널 작성 ////
            //string panelName = "Documentation";
            //RibbonPanel panel = RibbonUiHelper.GetOrCreateRibbonPanel(app, ribbonTabName, panelName);

            // 버튼이름, 리본에 표시될 텍스트, 어셈블리 경로, 실행될 커맨드 클래스 풀네임
            PushButtonData btn1 = new PushButtonData("SheetManagerCommand", "Sheets", Assembly.GetExecutingAssembly().Location, RevitCommandType<SheetManagerCommand>.FullName);
            //PushButtonData btn2 = new PushButtonData("QuantityCommand2", "Quantity2", Assembly.GetExecutingAssembly().Location, RevitCommandType<ModelingCommand1>.FullName);
            //PushButtonData btn3 = new PushButtonData("QuantityCommand3", "Quantity3", Assembly.GetExecutingAssembly().Location, RevitCommandType<ModelingCommand1>.FullName);
            //PushButtonData btn4 = new PushButtonData("QuantityCommand4", "Quantity4", Assembly.GetExecutingAssembly().Location, RevitCommandType<ModelingCommand1>.FullName);
            //PushButtonData btn5 = new PushButtonData("WebFamilyLibraryCommand", "Web\nLibrary", Assembly.GetExecutingAssembly().Location, RevitCommandType<WebFamilyLibraryCommand>.FullName);

            btn1.LargeImage = RibbonButtonImages.GetIcon("Sheet.png");
            //btn2.LargeImage = RibbonButtonImages.GetIcon("modeling.png");
            //btn3.LargeImage = RibbonButtonImages.GetIcon("modeling.png");
            //btn4.LargeImage = RibbonButtonImages.GetIcon("modeling.png");
            //btn5.LargeImage = RibbonButtonImages.GetIcon("revit.png");

            panel.AddItem(btn1);
            //panel.AddItem(btn2);
            //panel.AddItem(btn3);
            //panel.AddItem(btn4);
            //panel.AddItem(btn5);
        }
    }
}
