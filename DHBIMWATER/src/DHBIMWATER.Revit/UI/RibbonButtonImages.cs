using DHBIMWATER.Infrastructure.Logging;
using DHBIMWATER.UI.Constants;
using System.Windows.Media.Imaging;

namespace DHBIMWATER.Revit.UI
{
    internal static class RibbonButtonImages
    {
        internal static BitmapImage? GetIcon(string imageName)
        {
            try
            {
                Uri imageUri = new Uri($"{UIConstants.IconBasePath}{imageName}", UriKind.Absolute);

                BitmapImage btnImage = new BitmapImage();
                btnImage.BeginInit();
                btnImage.UriSource = imageUri;
                btnImage.DecodePixelWidth = 32;  // Revit 권장 크기로 리사이징
                btnImage.CacheOption = BitmapCacheOption.OnLoad; // 즉시 로드
                btnImage.EndInit();
                btnImage.Freeze(); // 중요: 다른 스레드에서 사용 가능하도록
                return btnImage;
            }
            catch (Exception ex)
            {
                LogManager.Logger.Error($"아이콘 로드 실패: {imageName}. 예외: {ex.Message}");
            }

            return null;
        }
    }
}
