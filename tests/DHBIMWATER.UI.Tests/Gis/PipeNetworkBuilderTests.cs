using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Gis;
using Xunit;

namespace DHBIMWATER.UI.Tests.Gis;

public class PipeNetworkBuilderTests
{
    private const double Tol = PipeNetworkBuilder.DefaultSnapToleranceM;   // 10mm

    private static PipeAlignment Alignment(double diameterMm, params Point3D[] vertices) =>
        new(vertices, "상수", diameterMm, "test.shp", "1", new Dictionary<string, string>());

    private static NodeClassification At(IReadOnlyList<NodeClassification> nodes, double x, double y, double z = 0) =>
        nodes.Single(n => n.Position.DistanceTo(new Point3D(x, y, z)) < Tol);

    [Fact]
    public void Endpoint_touching_another_polyline_creates_tee()
    {
        // 본관 (0,0)-(10,0), 지관 끝점이 본관 중간 (5,0)에 붙는다.
        var main = Alignment(300, new Point3D(0, 0, 0), new Point3D(10, 0, 0));
        var branch = Alignment(300, new Point3D(5, 0, 0), new Point3D(5, 5, 0));

        var graph = PipeNetworkBuilder.Build(new[] { main, branch }, Tol);
        var nodes = PipeNetworkClassifier.Classify(graph);

        Assert.Equal(3, graph.Edges.Count);            // 본관이 (5,0)에서 분할됨
        var tee = At(nodes, 5, 0);
        Assert.Equal(3, tee.Degree);
        Assert.Equal(NodeKind.Tee, tee.Kind);
    }

    [Fact]
    public void Smaller_branch_diameter_is_classified_as_reducing_tee()
    {
        var main = Alignment(300, new Point3D(0, 0, 0), new Point3D(10, 0, 0));
        var branch = Alignment(100, new Point3D(5, 0, 0), new Point3D(5, 5, 0));

        var nodes = PipeNetworkClassifier.Classify(PipeNetworkBuilder.Build(new[] { main, branch }, Tol));

        var tee = At(nodes, 5, 0);
        Assert.Equal(NodeKind.ReducingTee, tee.Kind);
        Assert.Equal(300, tee.MaxDiameterMm);
        Assert.Equal(100, tee.MinDiameterMm);
    }

    [Fact]
    public void Crossing_polylines_split_both_sides_into_cross()
    {
        var a = Alignment(300, new Point3D(0, 0, 0), new Point3D(10, 0, 0));
        var b = Alignment(300, new Point3D(5, -5, 0), new Point3D(5, 5, 0));

        var graph = PipeNetworkBuilder.Build(new[] { a, b }, Tol);
        var nodes = PipeNetworkClassifier.Classify(graph);

        Assert.Equal(4, graph.Edges.Count);             // 양쪽 모두 분할
        var cross = At(nodes, 5, 0);
        Assert.Equal(4, cross.Degree);
        Assert.Equal(NodeKind.Cross, cross.Kind);
    }

    [Fact]
    public void Endpoint_within_snap_tolerance_is_merged()
    {
        // 5mm 어긋난 끝점은 허용오차(10mm) 안이므로 같은 절점으로 병합된다.
        var a = Alignment(300, new Point3D(0, 0, 0), new Point3D(10, 0, 0));
        var b = Alignment(300, new Point3D(10.005, 0, 0), new Point3D(10.005, 5, 0));

        var nodes = PipeNetworkClassifier.Classify(PipeNetworkBuilder.Build(new[] { a, b }, Tol));

        Assert.Equal(3, nodes.Count);
        Assert.Equal(2, At(nodes, 10, 0).Degree);
    }

    [Fact]
    public void Endpoint_beyond_snap_tolerance_stays_disconnected()
    {
        // 50mm 어긋나면 접속되지 않고 단부 2개로 남아 진단 대상이 된다.
        var a = Alignment(300, new Point3D(0, 0, 0), new Point3D(10, 0, 0));
        var b = Alignment(300, new Point3D(10.05, 0, 0), new Point3D(10.05, 5, 0));

        var nodes = PipeNetworkClassifier.Classify(PipeNetworkBuilder.Build(new[] { a, b }, Tol));

        Assert.Equal(4, nodes.Count);
        Assert.Equal(4, nodes.Count(x => x.Kind == NodeKind.EndPoint));
    }

    [Fact]
    public void Vertically_separated_crossing_is_not_a_junction()
    {
        // 표고가 2m 다른 입체교차는 3D 거리 기준이므로 분기가 아니다.
        var a = Alignment(300, new Point3D(0, 0, 0), new Point3D(10, 0, 0));
        var b = Alignment(300, new Point3D(5, -5, 2), new Point3D(5, 5, 2));

        var graph = PipeNetworkBuilder.Build(new[] { a, b }, Tol);

        Assert.Equal(2, graph.Edges.Count);
        Assert.All(PipeNetworkClassifier.Classify(graph), x => Assert.Equal(NodeKind.EndPoint, x.Kind));
    }

    [Fact]
    public void Polyline_corner_reports_deflection_angle()
    {
        // 동쪽 → 북동 45°로 꺾이는 절점.
        var alignment = Alignment(300, new Point3D(0, 0, 0), new Point3D(10, 0, 0), new Point3D(20, 10, 0));

        var nodes = PipeNetworkClassifier.Classify(PipeNetworkBuilder.Build(new[] { alignment }, Tol));

        var corner = At(nodes, 10, 0);
        Assert.Equal(NodeKind.Bend, corner.Kind);
        Assert.Equal(45d, corner.DeflectionDeg, 9);
    }

    [Fact]
    public void Straight_polyline_vertex_has_zero_deflection()
    {
        var alignment = Alignment(300, new Point3D(0, 0, 0), new Point3D(10, 0, 0), new Point3D(20, 0, 0));

        var nodes = PipeNetworkClassifier.Classify(PipeNetworkBuilder.Build(new[] { alignment }, Tol));

        var middle = At(nodes, 10, 0);
        Assert.Equal(NodeKind.Straight, middle.Kind);
        Assert.Equal(0d, middle.DeflectionDeg, 9);
    }

    [Fact]
    public void Duplicate_vertices_do_not_produce_zero_length_edges()
    {
        var alignment = Alignment(300, new Point3D(0, 0, 0), new Point3D(0, 0, 0), new Point3D(10, 0, 0));

        var graph = PipeNetworkBuilder.Build(new[] { alignment }, Tol);

        Assert.Single(graph.Edges);
        Assert.All(graph.Edges, e => Assert.True(e.Length > Tol));
    }

    [Fact]
    public void Five_branches_are_flagged_for_review()
    {
        var center = new Point3D(0, 0, 0);
        var alignments = new[]
        {
            Alignment(300, center, new Point3D(10, 0, 0)),
            Alignment(300, center, new Point3D(-10, 0, 0)),
            Alignment(300, center, new Point3D(0, 10, 0)),
            Alignment(300, center, new Point3D(0, -10, 0)),
            Alignment(300, center, new Point3D(0, 0, 10))
        };

        var nodes = PipeNetworkClassifier.Classify(PipeNetworkBuilder.Build(alignments, Tol));

        Assert.Equal(NodeKind.TooManyBranches, At(nodes, 0, 0).Kind);
    }
}
