using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Piping;
using Xunit;

namespace DHBIMWATER.Core.Tests.Piping;

public sealed class PipeTopologyBuilderTests
{
    [Fact]
    public void AddSegment_DoesNotBendExistingEdge_WhenOnlyInfiniteExtensionIntersects()
    {
        var network = new PipeNetwork();
        network.AddSegment(new Point2D(-1000, 0), new Point2D(1000, 0));

        network.AddSegment(new Point2D(0, 200), new Point2D(0, 400));

        Assert.Equal(2, network.Edges.Count);
        Assert.Equal(4, network.Nodes.Count);
        Assert.Contains(network.Edges, edge => IsEdge(network, edge, new(-1000, 0), new(1000, 0)));
    }

    [Fact]
    public void AddSegment_DoesNotConnectSeparatedCollinearEdges()
    {
        var network = new PipeNetwork();
        network.AddSegment(new Point2D(0, 0), new Point2D(1000, 0));

        network.AddSegment(new Point2D(1200, 0), new Point2D(1400, 0));

        Assert.Equal(2, network.Edges.Count);
        Assert.Equal(4, network.Nodes.Count);
    }

    [Fact]
    public void AddSegment_SplitsBothEdgesAtActualIntersection()
    {
        var network = new PipeNetwork();
        network.AddSegment(new Point2D(-1000, 0), new Point2D(1000, 0));

        network.AddSegment(new Point2D(0, -1000), new Point2D(0, 1000));

        Assert.Equal(4, network.Edges.Count);
        var intersection = Assert.Single(network.Nodes, x => x.Position.DistanceTo(new Point2D(0, 0)) <= 1e-6);
        Assert.Equal(4, intersection.Degree);
        Assert.Equal(NodeKind.Cross, intersection.NodeKind);
    }

    [Fact]
    public void AddSegment_DoesNotReuseNearbyUnrelatedEndpointForIntersection()
    {
        var network = new PipeNetwork();
        network.AddSegment(new Point2D(50, 50), new Point2D(250, 50));
        network.AddSegment(new Point2D(-1000, 0), new Point2D(1000, 0));

        network.AddSegment(new Point2D(0, -1000), new Point2D(0, 1000));

        var intersection = Assert.Single(network.Nodes, x => x.Position.DistanceTo(new Point2D(0, 0)) <= 1e-6);
        Assert.Equal(new Point2D(0, 0), intersection.Position);
        Assert.Contains(network.Nodes, x => x.Position == new Point2D(50, 50));
    }

    [Fact]
    public void TeeResolver_ReturnsCollinearMainAndOrthogonalBranch()
    {
        var network = new PipeNetwork();
        network.AddSegment(new Point2D(-1000, 0), new Point2D(1000, 0));
        network.AddSegment(new Point2D(0, 0), new Point2D(0, 1000));
        var definition = network.ToDefinition(100, PipeOutputMode.MepPipe, new Point2D(0, 0));
        var tee = Assert.Single(definition.Nodes, x => x.NodeKind == NodeKind.Tee);

        var order = Assert.IsType<PipeTeeLegOrder>(PipeTeeResolver.Resolve(tee, definition.Edges));

        var branch = Assert.Single(definition.Edges, x => x.Id == order.BranchEdgeId);
        Assert.Equal(0, branch.Start.X, 6);
        Assert.Equal(0, branch.End.X, 6);
    }

    [Fact]
    public void TeeResolver_RejectsNonOrthogonalThreeWayBranch()
    {
        var network = new PipeNetwork();
        network.AddSegment(new Point2D(-1000, 0), new Point2D(1000, 0));
        network.AddSegment(new Point2D(0, 0), new Point2D(1000, 1000));
        var definition = network.ToDefinition(100, PipeOutputMode.MepPipe, new Point2D(0, 0));
        var tee = Assert.Single(definition.Nodes, x => x.NodeKind == NodeKind.Tee);

        Assert.Null(PipeTeeResolver.Resolve(tee, definition.Edges));
    }

    private static bool IsEdge(PipeNetwork network, PipeEdge edge, Point2D expectedStart, Point2D expectedEnd)
    {
        var start = network.Nodes.Single(x => x.Id == edge.StartNodeId).Position;
        var end = network.Nodes.Single(x => x.Id == edge.EndNodeId).Position;
        return start == expectedStart && end == expectedEnd || start == expectedEnd && end == expectedStart;
    }
}

public sealed class PipeTopologyBuilderSnapPriorityTests
{
    [Fact]
    public void FindSnapPoint_PrefersEndpointOverCloserNearestPoint()
    {
        var network = SingleHorizontalEdge();
        var cursor = new Point2D(Math.Sqrt(2700), 30);

        var result = network.FindSnapPoint(cursor, PipeSnapMode.Endpoint | PipeSnapMode.Nearest);

        Assert.NotNull(result);
        Assert.Equal(PipeSnapKind.Endpoint, result.Value.Kind);
        Assert.Equal(new Point2D(0, 0), result.Value.Point);
        Assert.Equal(60, result.Value.Distance, 6);
    }

    [Fact]
    public void FindSnapPoint_FallsBackToNearestWhenEndpointIsOutsideTolerance()
    {
        var network = SingleHorizontalEdge();

        var result = network.FindSnapPoint(new Point2D(150, 30), PipeSnapMode.Endpoint | PipeSnapMode.Nearest);

        Assert.NotNull(result);
        Assert.Equal(PipeSnapKind.Nearest, result.Value.Kind);
        Assert.Equal(new Point2D(150, 0), result.Value.Point);
    }

    [Fact]
    public void FindSnapPoint_ClassifiesDegreeThreeNodeAsIntersectionInEndpointMode()
    {
        var network = new PipeNetwork();
        network.AddSegment(new Point2D(-1000, 0), new Point2D(1000, 0));
        network.AddSegment(new Point2D(0, 0), new Point2D(0, 1000));

        var result = network.FindSnapPoint(new Point2D(20, 20), PipeSnapMode.Endpoint);

        Assert.NotNull(result);
        Assert.Equal(PipeSnapKind.Intersection, result.Value.Kind);
        Assert.Equal(new Point2D(0, 0), result.Value.Point);
    }

    [Fact]
    public void FindSnapPoint_PrefersMidpointOverNearestPoint()
    {
        var network = SingleHorizontalEdge();

        var result = network.FindSnapPoint(new Point2D(500, 30), PipeSnapMode.Midpoint | PipeSnapMode.Nearest);

        Assert.NotNull(result);
        Assert.Equal(PipeSnapKind.Midpoint, result.Value.Kind);
        Assert.Equal(new Point2D(500, 0), result.Value.Point);
    }

    [Fact]
    public void FindSnapPoint_PrefersQuadrantOverNearestPoint()
    {
        var network = SingleHorizontalEdge();

        var result = network.FindSnapPoint(new Point2D(250, 30), PipeSnapMode.Quadrant | PipeSnapMode.Nearest);

        Assert.NotNull(result);
        Assert.Equal(PipeSnapKind.Quadrant, result.Value.Kind);
        Assert.Equal(new Point2D(250, 0), result.Value.Point);
    }

    [Fact]
    public void FindSnapPoint_ReturnsNullWhenEveryCandidateIsOutsideTolerance()
    {
        var network = SingleHorizontalEdge();

        var result = network.FindSnapPoint(new Point2D(500, 200),
            PipeSnapMode.Endpoint | PipeSnapMode.Intersection | PipeSnapMode.Midpoint | PipeSnapMode.Quadrant | PipeSnapMode.Nearest);

        Assert.Null(result);
    }

    private static PipeNetwork SingleHorizontalEdge()
    {
        var network = new PipeNetwork();
        network.AddSegment(new Point2D(0, 0), new Point2D(1000, 0));
        return network;
    }
}
