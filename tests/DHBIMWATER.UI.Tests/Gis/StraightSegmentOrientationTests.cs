using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Gis;
using Xunit;

namespace DHBIMWATER.UI.Tests.Gis;

public class StraightSegmentOrientationTests
{
    [Fact]
    public void Chain_start_uses_own_direction_for_both_ends()
    {
        var result = StraightSegmentOrientation.Resolve(
            new Point3D(1, 2, 3), new Point3D(5, 8, 10),
            previousEnd: null, previousOwnDirection: null);

        Assert.Equal(new Vector3D(4, 6, 7), result.Own);
        Assert.Equal(result.Own, result.Start);
    }

    [Fact]
    public void Joined_segment_inherits_previous_own_direction_only_at_start()
    {
        var previousOwn = new Vector3D(4, 0, 0);
        var result = StraightSegmentOrientation.Resolve(
            new Point3D(4, 0, 0), new Point3D(4, 3, 2),
            new Point3D(4, 0, 0), previousOwn);

        Assert.Equal(previousOwn, result.Start);
        Assert.Equal(new Vector3D(0, 3, 2), result.Own);
        Assert.NotEqual(result.Start, result.Own);
    }

    [Fact]
    public void Trimmed_gap_does_not_inherit_previous_direction()
    {
        var result = StraightSegmentOrientation.Resolve(
            new Point3D(4, 0, 0), new Point3D(4, 3, 2),
            new Point3D(3.99, 0, 0), new Vector3D(4, 0, 0));

        Assert.Equal(new Vector3D(0, 3, 2), result.Own);
        Assert.Equal(result.Own, result.Start);
    }
}
