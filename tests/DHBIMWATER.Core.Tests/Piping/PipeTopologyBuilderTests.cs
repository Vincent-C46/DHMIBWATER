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

    private static bool IsEdge(PipeNetwork network, PipeEdge edge, Point2D expectedStart, Point2D expectedEnd)
    {
        var start = network.Nodes.Single(x => x.Id == edge.StartNodeId).Position;
        var end = network.Nodes.Single(x => x.Id == edge.EndNodeId).Position;
        return start == expectedStart && end == expectedEnd || start == expectedEnd && end == expectedStart;
    }
}
