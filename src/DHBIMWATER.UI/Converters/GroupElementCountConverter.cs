using DHBIMWATER.Core.Quantity;
using System.Globalization;
using System.Windows.Data;

namespace DHBIMWATER.UI.Converters
{
    /// <summary>
    /// CollectionViewGroup → 그룹 내 고유 ElementId 개수
    /// GroupStyle 헤더에서 "W1 — 3개 요소" 처럼 표시할 때 사용
    /// </summary>
    public class GroupElementCountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CollectionViewGroup group)
            {
                return group.Items
                    .OfType<QuantityItem>()
                    .Select(i => i.ElementId)
                    .Distinct()
                    .Count();
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
