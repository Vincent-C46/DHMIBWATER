using DHBIMWATER.Core.Quantity;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DHBIMWATER.UI.Converters
{
    public class QuantityStatusToLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (QuantityStatus)value switch
            {
                QuantityStatus.Auto     => "자동",
                QuantityStatus.Modified => "수정",
                QuantityStatus.Manual   => "수동",
                _                       => string.Empty
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class QuantityStatusToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush AutoBrush     = new(Colors.White);
        private static readonly SolidColorBrush ModifiedBrush = new(Color.FromRgb(0xD4, 0xA0, 0x17)); // #D4A017
        private static readonly SolidColorBrush ManualBrush   = new(Color.FromRgb(0xCD, 0x5C, 0x5C)); // IndianRed

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (QuantityStatus)value switch
            {
                QuantityStatus.Modified => ModifiedBrush,
                QuantityStatus.Manual   => ManualBrush,
                _                       => AutoBrush
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
