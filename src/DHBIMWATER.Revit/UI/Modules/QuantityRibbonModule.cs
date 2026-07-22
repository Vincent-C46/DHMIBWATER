using Autodesk.Revit.UI;
using DHBIMWATER.Infrastructure.Logging;
using System.Reflection;

namespace DHBIMWATER.Revit.UI.Modules
{
    /// <summary>
    /// 수량산출 리본 패널.
    /// 컴파일 타임 참조 없이 <see cref="PushButtonData"/>의 어셈블리 경로 + 클래스 풀네임 문자열로
    /// DHBoost 커맨드를 직접 실행한다.
    /// (참조를 걸면 DHBoost.Infrastructure 타입이 libs/DHBoost/DHBoost.Combined.dll 과 중복 로드되어 타입 충돌이 난다)
    /// 타이틀바 브랜딩은 DHBoost 쪽 *ForWater 파생 커맨드가 담당한다.
    /// </summary>
    internal class QuantityRibbonModule : IRibbonModule
    {
        /// <summary>DHBoost 애드인 폴더명. DHBIMWATER와 형제 폴더로 배포된다는 전제.</summary>
        private const string DHBoostFolderName = "DHBoost";
        private const string DHBoostAssemblyName = "DHBoost.Revit.dll";

        private const string QuantityCommandFullName = "DHBoost.Revit.Commands.Quantity.QuantityCommandForWater";
        private const string QuantitySettingCommandFullName = "DHBoost.Revit.Commands.Quantity.QuantitySettingCommandForWater";

        public IEnumerable<RibbonItem> Build(UIControlledApplication app, string ribbonTabName)
        {
            string? dhBoostPath = ResolveDHBoostAssemblyPath();

            if (dhBoostPath is null)
            {
                // DHBoost 미설치 → 깨진 버튼을 만들지 않고 패널 자체를 만들지 않는다
                LogManager.Logger.Error(
                    $"DHBoost 애드인을 찾을 수 없어 수량산출 버튼을 생성하지 않습니다. 기대 경로: <Addins>\\{DHBoostFolderName}\\{DHBoostAssemblyName}");
                return [];
            }

            RibbonPanel panel = app.CreateRibbonPanel(ribbonTabName, "Quantity");

            PushButtonData quantitySettingsBtn = new PushButtonData(
                "QuantitySettingCommand", "수량산출\n설정", dhBoostPath, QuantitySettingCommandFullName);
            quantitySettingsBtn.LargeImage = RibbonButtonImages.GetIcon("Quantity.png");

            PushButtonData quantityBtn = new PushButtonData(
                "QuantityCommand", "수량산출", dhBoostPath, QuantityCommandFullName);
            quantityBtn.LargeImage = RibbonButtonImages.GetIcon("Quantity.png");

            return [panel.AddItem(quantitySettingsBtn), panel.AddItem(quantityBtn)];
        }

        /// <summary>
        /// DHBoost.Revit.dll 경로를 찾는다.
        /// 배포 구조가 <c>%APPDATA%\Autodesk\Revit\Addins\&lt;버전&gt;\{DHBIMWATER,DHBoost}\</c> 형제 폴더이므로,
        /// 자기 어셈블리 위치 기준 상대 경로로 해석해 Revit 버전을 하드코딩하지 않는다.
        /// </summary>
        private static string? ResolveDHBoostAssemblyPath()
        {
            string? selfFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string? addinsRoot = Path.GetDirectoryName(selfFolder);

            if (addinsRoot is null) return null;

            string candidate = Path.Combine(addinsRoot, DHBoostFolderName, DHBoostAssemblyName);
            return File.Exists(candidate) ? candidate : null;
        }
    }
}
