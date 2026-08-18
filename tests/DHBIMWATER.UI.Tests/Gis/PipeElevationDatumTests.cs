using DHBIMWATER.Core.Gis;
using Xunit;

namespace DHBIMWATER.UI.Tests.Gis;

public class PipeElevationDatumTests
{
    private static readonly StraightPipeSpec Dn800 = new(PipeKindCatalog.Water1, 800, 842, 14.3);

    [Theory]
    [InlineData(ZDatum.Centerline, 0)]
    [InlineData(ZDatum.Invert, 406.7)]
    [InlineData(ZDatum.Crown, -406.7)]
    [InlineData(ZDatum.OutsideBottom, 421)]
    [InlineData(ZDatum.OutsideTop, -421)]
    public void Offset_matches_dn800_example(ZDatum datum, double expectedMm)
        => Assert.Equal(expectedMm, PipeElevationDatum.OffsetMm(datum, Dn800, 800), 6);

    [Fact]
    public void Centerline_is_zero_without_spec()
        => Assert.Equal(0, PipeElevationDatum.OffsetMm(ZDatum.Centerline, null, 800));

    [Fact]
    public void Missing_spec_uses_half_nominal_diameter_for_invert()
        => Assert.Equal(400, PipeElevationDatum.OffsetMm(ZDatum.Invert, null, 800));

    [Fact]
    public void Zero_thickness_makes_inner_and_outer_radius_equal()
    {
        var spec = new StraightPipeSpec(PipeKindCatalog.Water1, 800, 842, 0);
        Assert.Equal(
            PipeElevationDatum.OffsetMm(ZDatum.OutsideBottom, spec, 800),
            PipeElevationDatum.OffsetMm(ZDatum.Invert, spec, 800));
    }

    [Theory]
    [InlineData(ZDatum.Centerline, false)]
    [InlineData(ZDatum.Invert, true)]
    [InlineData(ZDatum.Crown, true)]
    [InlineData(ZDatum.OutsideTop, true)]
    [InlineData(ZDatum.OutsideBottom, true)]
    public void Needs_spec_only_returns_false_for_centerline(ZDatum datum, bool expected)
        => Assert.Equal(expected, PipeElevationDatum.NeedsSpec(datum));
}
