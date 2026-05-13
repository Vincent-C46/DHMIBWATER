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
            //new WindowInteropHelper(this).Owner = revitHandle();
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is not TabControl) return;
            // SizeToContent를 한 번 Manual로 리셋 후 재설정해야 작아지는 방향으로도 동작함
            SizeToContent = SizeToContent.Manual;
            Width = double.NaN;
            SizeToContent = SizeToContent.Width;
        }
    }
}
