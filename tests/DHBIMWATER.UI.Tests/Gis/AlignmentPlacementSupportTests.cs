using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Gis;
using DHBIMWATER.Infrastructure.Repositories.Gis;
using Xunit;

namespace DHBIMWATER.UI.Tests.Gis;

public class AlignmentPlacementSupportTests
{
    [Fact]
    public void ReferencePoint_takes_plan_coordinates_of_first_vertex_and_drops_elevation()
    {
        var alignments = new[]
        {
            new PipeAlignment(
                new[] { new Point3D(123.5, -456.5, 10.5) },
                "상수", 100, "test.shp", "1", new Dictionary<string, string>())
        };

        var reference = AlignmentReferencePoint.FromFirstVertex(alignments);

        // 기준점은 PBP 위치에 그대로 대응하므로 정수 반올림하지 않는다(소수 5자리 오차 제거만).
        // 표고는 기준점에 포함하지 않고 정점 Z로만 결정한다.
        Assert.NotNull(reference);
        Assert.Equal(123.5, reference!.Value.X);
        Assert.Equal(-456.5, reference.Value.Y);
    }

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
    public void SampleSegments_breaks_at_polyline_vertex_not_aligned_to_interval()
    {
        // 절점(5,0,0)이 간격 6의 배수가 아니어서 기존 로직은 코너를 가로지르는 현을 만들었다.
        // 간격은 절점마다 리셋되므로 5m짜리 두 구간은 각각 통째로 하나의 세그먼트가 된다.
        var vertices = new[] { new Point3D(0, 0, 0), new Point3D(5, 0, 0), new Point3D(5, 5, 0) };

        var segments = AlignmentIntervalSampler.SampleSegments(vertices, 6);

        Assert.Equal(new[] { 0d, 5d }, segments.Select(x => x.DistanceFromStart));
        Assert.Equal(5, segments[0].End.X);
        Assert.Equal(0, segments[0].End.Y);   // 절점에서 정확히 분절
        Assert.Equal(5, segments[1].End.Y);   // 종점
    }

    [Fact]
    public void SampleSegments_restarts_interval_at_each_vertex()
    {
        // 앞 구간(7m)의 잔여 1m가 다음 구간으로 전파되면 안 된다.
        // 전체 누적거리 기준이면 두 번째 구간이 5m/3m로 끊겼지만, 절점 기준 리셋에서는 6m/2m가 된다.
        var vertices = new[] { new Point3D(0, 0, 0), new Point3D(7, 0, 0), new Point3D(7, 8, 0) };

        var segments = AlignmentIntervalSampler.SampleSegments(vertices, 6);

        Assert.Equal(new[] { 0d, 6d, 7d, 13d }, segments.Select(x => x.DistanceFromStart));
        Assert.Equal(new[] { 6d, 1d, 6d, 2d }, segments.Select(x => Math.Round(x.Start.DistanceTo(x.End), 9)));
    }

    [Fact]
    public void SampleSegments_does_not_emit_zero_length_when_vertex_matches_interval()
    {
        // 절점 거리(6)와 간격 배수(6)가 겹쳐도 길이 0 세그먼트가 생기면 안 된다.
        var vertices = new[] { new Point3D(0, 0, 0), new Point3D(6, 0, 0), new Point3D(6, 6, 0) };

        var segments = AlignmentIntervalSampler.SampleSegments(vertices, 6);

        Assert.Equal(2, segments.Count);
        Assert.All(segments, x => Assert.True(x.Start.DistanceTo(x.End) > 1e-9));
    }

    [Fact]
    public void SampleSegments_covers_full_length_without_gap()
    {
        var vertices = new[] { new Point3D(0, 0, 0), new Point3D(5, 0, 0), new Point3D(5, 5, 0), new Point3D(12, 5, 0) };

        var segments = AlignmentIntervalSampler.SampleSegments(vertices, 6);

        var length = segments.Sum(x => x.Start.DistanceTo(x.End));
        Assert.Equal(17d, length, 9);                        // 5 + 5 + 7
        Assert.Equal(0d, segments[0].DistanceFromStart);
        Assert.Equal(12, segments[^1].End.X);
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
