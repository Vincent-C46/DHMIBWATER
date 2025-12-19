using Autodesk.Revit.UI;
using System.Windows.Media.Imaging;

namespace DHBIMWATER.Revit.UI
{
    internal class RibbonBuilder
    {
        internal static void CreateRibbonPanel(UIControlledApplication app)
        {
            // DHBIMWATER 리본 패널 생성
            string tabName = "DHBIMWATER";
            app.CreateRibbonTab(tabName);

            // 모델링 패널
            RibbonPanel modelingPanel = app.CreateRibbonPanel(tabName, "모델링");

            // 모델링1 버튼
            PushButtonData btnModeling1 = new PushButtonData(
                "ModelingCommand1",
                "모델링1",
                typeof(RibbonBuilder).Assembly.Location,
                "DHBIMWATER.Revit.Commands.ModelingCommand1"
            );

            // 아이콘 설정 (AddItem 전에 설정해야 함!)
            var iconImage = GetIcon("modeling.png");
            if (iconImage != null)
            {
                btnModeling1.LargeImage = iconImage;
                btnModeling1.Image = iconImage;
            }

            // 버튼을 패널에 추가
            modelingPanel.AddItem(btnModeling1);

            // 수량 산출 패널
            RibbonPanel quantityPanel = app.CreateRibbonPanel(tabName, "수량산출");

            // 유틸리티 패널
            RibbonPanel utilityPanel = app.CreateRibbonPanel(tabName, "유틸리티");
        }

        private static BitmapImage? GetIcon(string iconPath)
        {
            try
            {
                // 파일 경로로 직접 로드
                var assemblyFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                var iconFilePath = Path.Combine(assemblyFolder ?? "", "Resources", "Icons", iconPath);

                if (File.Exists(iconFilePath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(iconFilePath, UriKind.Absolute);
                    bitmap.DecodePixelWidth = 32;  // Revit 권장 크기로 리사이징
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze(); // 중요: 다른 스레드에서 사용 가능하도록
                    return bitmap;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Icon file not found: {iconFilePath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Icon load failed: {ex.Message}");
            }

            return null;
        }

    }
}
