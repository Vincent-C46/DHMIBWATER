using DHBIMWATER.UI.ViewModels.Quantity;
using System.Windows;

namespace DHBIMWATER.UI.Views.Quantity
{
    /// <summary>
    /// QuantityCalculationView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class QuantityCalculationView : Window
    {
        public QuantityCalculationView(QuantityCalculationViewModel quantityCalculationViewModel)
        {
            InitializeComponent();
            DataContext = quantityCalculationViewModel;
        }
    }
}
