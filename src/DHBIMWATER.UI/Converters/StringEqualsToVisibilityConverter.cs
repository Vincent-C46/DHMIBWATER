using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DHBIMWATER.UI.Converters
{
    /// <summary>값이 ConverterParameter(문자열)와 같으면 Visible, 아니면 Collapsed. 관종·직경 콤보박스가 "&lt;수동&gt;"일 때만 옆 텍스트박스를 보여주는 용도.</summary>
    public class StringEqualsToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => Equals(value?.ToString(), parameter?.ToString()) ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
