using DHBIMWATER.UI.ViewModels.Quantity;
using System.Windows;
using System.Windows.Controls;

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

        private void VariableTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
                tb.SelectAll();
        }

        private void VariableTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // 숫자, 소수점, 마이너스만 허용
            e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(e.Text, @"^[0-9.\-]$");
        }
    }
}
