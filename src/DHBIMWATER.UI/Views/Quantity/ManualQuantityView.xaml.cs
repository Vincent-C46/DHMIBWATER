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
            if (sender is not TextBox tb) return;

            // 입력 후 전체 문자열 기준으로 검증 (소수점 중복 등 차단)
            var newText = tb.Text.Remove(tb.SelectionStart, tb.SelectionLength)
                                  .Insert(tb.SelectionStart, e.Text);
            e.Handled = !_validNumericRegex.IsMatch(newText);
        }

        private void VariableTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox tb) return;

            // 포커스 벗어날 때 정규화: ".5" → "0.5", "1." → "1", 빈값/불완전 → "0"
            if (!double.TryParse(tb.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                d = 0;
            tb.Text = d.ToString(CultureInfo.InvariantCulture);
        }
    }
}
