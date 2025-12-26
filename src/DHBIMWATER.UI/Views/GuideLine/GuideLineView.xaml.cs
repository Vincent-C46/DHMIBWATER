using DHBIMWATER.UI.ViewModels.GuideLine;
using System.Windows;

namespace DHBIMWATER.UI.Views.GuideLine
{
    /// <summary>
    /// GuideLineView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class GuideLineView : Window
    {
        public GuideLineView(GuideLineViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
