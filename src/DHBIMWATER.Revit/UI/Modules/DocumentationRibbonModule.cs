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
        public IEnumerable<RibbonItem> Build(UIControlledApplication app, string ribbonTabName)
        {
            RibbonPanel panel = app.CreateRibbonPanel(ribbonTabName, "Documentation");

            // 버튼이름, 리본에 표시될 텍스트, 어셈블리 경로, 실행될 커맨드 클래스 풀네임
            PushButtonData btn1 = new PushButtonData("SheetManagerCommand", "Sheets", Assembly.GetExecutingAssembly().Location, RevitCommandType<SheetManagerCommand>.FullName);
            btn1.LargeImage = RibbonButtonImages.GetIcon("Sheet.png");

            return [panel.AddItem(btn1)];
        }
    }
}
