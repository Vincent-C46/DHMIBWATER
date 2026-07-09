using DHBIMWATER.UI.ViewModels.Modeling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DHBIMWATER.UI.Views.Modeling
{
    /// <summary>
    /// PumpingStationView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PumpingStationView : Window
    {
        public PumpingStationView(PumpingStationViewModel pumpingStationViewModel)
        {
            InitializeComponent();
            DataContext = pumpingStationViewModel;
            pumpingStationViewModel.CloseAction = Close;
            ContentRendered += (s, e) =>
            {
                SizeToContent = SizeToContent.Manual;
                SizeToContent = SizeToContent.Height;
            };
        }
        private void OnParameterFocused(object sender, RoutedEventArgs e)
        {
            if (DataContext is not PumpingStationViewModel vm) return;

            // 포커스 받은 요소에서 시각 트리를 따라 올라가며 string Tag(힌트 키)를 찾음
            // → TextBox 뿐 아니라 ComboBox(θ 등)도 호환
            var element = e.OriginalSource as DependencyObject;
            while (element != null)
            {
                if (element is FrameworkElement fe && fe.Tag is string key && !string.IsNullOrEmpty(key))
                {
                    vm.SetHint(key);
                    return;
                }
                element = VisualTreeHelper.GetParent(element);
            }
        }
    }
}
