using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Gis;
using DHBIMWATER.Infrastructure.Repositories.Gis;
using Xunit;

namespace DHBIMWATER.UI.Tests.Gis;

public class AlignmentPlacementSupportTests
{
    [Fact]
    public void IntervalSampler_samples_points_and_terminal_segment_on_polyline()
    {
        var vertices = new[] { new Point3D(0, 0, 0), new Point3D(13, 0, 0) };

        var points = AlignmentIntervalSampler.SamplePoints(vertices, 6);
        var segments = AlignmentIntervalSampler.SampleSegments(vertices, 6);

        Assert.Equal(new[] { 0d, 6d, 12d }, points.Select(x => x.DistanceFromStart));
        Assert.Equal(3, segments.Count);
        Assert.Equal(12, segments[^1].Start.X);
        Assert.Equal(13, segments[^1].End.X);
    }

    [Fact]
    public void IntervalSampler_uses_arc_length_and_segment_tangent_at_corner()
    {
        var vertices = new[] { new Point3D(0, 0, 0), new Point3D(6, 0, 0), new Point3D(6, 6, 0) };

        var point = AlignmentIntervalSampler.SamplePoints(vertices, 9).Single(x => x.DistanceFromStart == 9);

        Assert.Equal(6, point.Position.X);
        Assert.Equal(3, point.Position.Y);
        Assert.Equal(0, point.Tangent.X);
        Assert.Equal(1, point.Tangent.Y);
    }

    [Fact]
    public void DxfReader_reads_lwpolyline_and_normalizes_xdata_diameter()
    {
        var path = Path.Combine(Path.GetTempPath(), $"alignment-{Guid.NewGuid():N}.dxf");
        File.WriteAllText(path, """
0
SECTION
2
ENTITIES
0
LWPOLYLINE
10
0
20
0
10
13
20
0
1001
DHBIMWATER
1000
KIND=상수, DIAMETER=100
0
ENDSEC
0
EOF
""");
        try
        {
            var result = new DxfAlignmentReader().Read(path);

            var alignment = Assert.Single(result.Features);
            Assert.Equal(2, alignment.Vertices.Count);
            Assert.Equal("상수_D100", alignment.Attributes["Diameter"]);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
