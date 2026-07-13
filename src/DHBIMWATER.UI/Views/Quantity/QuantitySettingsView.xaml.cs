using DHBIMWATER.UI.ViewModels.Quantity;
using System.Windows;

namespace DHBIMWATER.UI.Views.Quantity
{
    public partial class QuantitySettingsView : Window
    {
        public QuantitySettingsView(QuantitySettingsViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;

            vm.CloseRequested += _ => Close();
        }
    }
}
