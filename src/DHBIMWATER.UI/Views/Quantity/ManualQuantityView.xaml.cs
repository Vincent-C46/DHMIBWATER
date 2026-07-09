using DHBIMWATER.UI.ViewModels.Quantity;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace DHBIMWATER.UI.Views.Quantity
{
    public partial class ManualQuantityView : Window
    {
        private static readonly Regex _validNumericRegex = new(@"^-?[0-9]*\.?[0-9]*$", RegexOptions.Compiled);
        private static readonly Regex _integerRegex = new(@"^-?[0-9]*$", RegexOptions.Compiled);

        public ManualQuantityView(ManualQuantityViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;

            vm.CloseRequested += _ => Close();
        }
        private void VariableTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
                tb.SelectAll();
        }
        private bool IsEaUnit => (DataContext as ManualQuantityViewModel)?.Unit == "EA";

        private void VariableTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (sender is not TextBox tb) return;

            var newText = tb.Text.Remove(tb.SelectionStart, tb.SelectionLength)
                                  .Insert(tb.SelectionStart, e.Text);
            var regex = IsEaUnit ? _integerRegex : _validNumericRegex;
            e.Handled = !regex.IsMatch(newText);
        }
        private void VariableTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox tb) return;

            if (!double.TryParse(tb.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                d = 0;
            tb.Text = IsEaUnit
                ? ((long)Math.Round(d)).ToString()
                : d.ToString(CultureInfo.InvariantCulture);
        }
    }
}
