using System.Globalization;
using System.Windows.Media;
using DHBIMWATER.Core.Gis;
using DHBIMWATER.UI.Converters;
using Xunit;

namespace DHBIMWATER.UI.Tests.Converters;

public sealed class ZDatumConvertersTests
{
    [Fact]
    public void Enum_equals_brushes_are_frozen_for_cross_thread_use()
    {
        Brush? selected = null;
        Brush? unselected = null;
        Exception? sourceFailure = null;

        var sourceThread = new Thread(() =>
        {
            try
            {
                var converter = new EnumEqualsToBrushConverter();
                selected = Assert.IsAssignableFrom<Brush>(converter.Convert(
                    ZDatum.Invert, typeof(Brush), ZDatum.Invert, CultureInfo.InvariantCulture));
                unselected = Assert.IsAssignableFrom<Brush>(converter.Convert(
                    ZDatum.Invert, typeof(Brush), ZDatum.Crown, CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                sourceFailure = ex;
            }
        });

        sourceThread.SetApartmentState(ApartmentState.STA);
        sourceThread.Start();
        sourceThread.Join();

        Assert.Null(sourceFailure);
        Assert.NotNull(selected);
        Assert.NotNull(unselected);
        Assert.True(selected.IsFrozen);
        Assert.True(unselected.IsFrozen);

        Exception? targetFailure = null;
        var targetThread = new Thread(() =>
        {
            try
            {
                selected.GetAsFrozen();
                unselected.GetAsFrozen();
            }
            catch (Exception ex)
            {
                targetFailure = ex;
            }
        });

        targetThread.SetApartmentState(ApartmentState.STA);
        targetThread.Start();
        targetThread.Join();

        Assert.Null(targetFailure);
    }
}
