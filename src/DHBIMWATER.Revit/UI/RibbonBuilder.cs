using System.Windows.Forms;
using Autodesk.Revit.UI;
using DHBIMWATER.Infrastructure.Logging;
using DHBIMWATER.Revit.UI.Helper;
using DHBIMWATER.Revit.UI.Modules;

namespace DHBIMWATER.Revit.UI
{
    internal class RibbonBuilder
    {
        internal static void CreateRibbonPanel(UIControlledApplication app)
        {
            //DHBIMWATER 리본 패널 생성//
            string tabName = "DHBIMWATER";
            app.CreateRibbonTab(tabName);

            ////// 대상 텝에 작성 ////
            //string tabName = "DH-Water";
            //RibbonUiHelper.EnsureRibbonTab(app, tabName);


            // 리본 모듈 리스트 - 패널 생성 및 버튼 추가 담당
            List<IRibbonModule> modules = new List<IRibbonModule>()
            {
                new ModelingRibbonModule(),
                new DocumentationRibbonModule(),
                new UtilityRibbonModule()
            };

            // 각 모듈별로 빌드 호출
            foreach (IRibbonModule module in modules)
            {
                module.Build(app, tabName);                
            }
        }        
    }
}
