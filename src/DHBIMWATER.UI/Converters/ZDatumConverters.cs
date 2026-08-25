using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using DHBIMWATER.Core.Gis;

namespace DHBIMWATER.UI.Converters;

public sealed class ZDatumDisplayNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
    {
        ZDatum.Centerline => "중심선",
        ZDatum.Invert => "관저고 (내부 바닥)",
        ZDatum.Crown => "크라운 (내부 천장)",
        ZDatum.OutsideTop => "외부 맨 위",
        ZDatum.OutsideBottom => "외부 맨 아래",
        _ => value?.ToString() ?? string.Empty
    };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

public sealed class EnumEqualsToBrushConverter : IValueConverter
{
    private static readonly Brush SelectedBrush = CreateFrozenBrush(Color.FromRgb(229, 57, 53));
    private static readonly Brush UnselectedBrush = CreateFrozenBrush(Color.FromRgb(96, 105, 112));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.OrdinalIgnoreCase)
            ? SelectedBrush
            : UnselectedBrush;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;

    private static Brush CreateFrozenBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
