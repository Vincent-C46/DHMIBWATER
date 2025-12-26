using DHBIMWATER.UI.ViewModels.Modeling;
using System.Windows;

namespace DHBIMWATER.UI.Views.Modeling
{
    /// <summary>
    /// Modeling1View.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Modeling1View : Window
    {
        public Modeling1View(Modeling1ViewModel modeling1ViewModel)
        {
            InitializeComponent();
            DataContext = modeling1ViewModel;
        }
    }
}
