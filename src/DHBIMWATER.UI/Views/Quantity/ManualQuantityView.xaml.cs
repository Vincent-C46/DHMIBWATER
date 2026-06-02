using DHBIMWATER.UI.ViewModels.Quantity;
using System.Windows;

namespace DHBIMWATER.UI.Views.Quantity
{
    public partial class ManualQuantityView : Window
    {
        public ManualQuantityView(ManualQuantityViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;

            vm.CloseRequested += result => DialogResult = result;
        }
    }
}
