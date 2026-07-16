using DHBIMWATER.Core.Geometry;

namespace DHBIMWATER.Core.Piping;

public sealed class PipeTopologyBuilder
{
    public const double SnapTolerance = 100.0;
    private const double StraightAngleToleranceDegrees = 2.0;
    private readonly PipeNetwork _network;

    public PipeTopologyBuilder(PipeNetwork network) => _network = network;

    public Point2D SnapPoint(Point2D point)
    {
        var node = _network.Nodes.OrderBy(x => x.Position.DistanceTo(point)).FirstOrDefault();
        if (node is not null && node.Position.DistanceTo(point) <= SnapTolerance) return node.Position;
        var candidate = _network.Edges.Select(edge =>
        {
            var start = _network.FindNode(edge.StartNodeId)!.Position;
            var end = _network.FindNode(edge.EndNodeId)!.Position;
            var t = Math.Clamp(Segment2D.ParameterOnSegment(point, start, end), 0, 1);
            var projected = new Point2D(start.X + (end.X - start.X) * t, start.Y + (end.Y - start.Y) * t);
            return (projected, distance: projected.DistanceTo(point));
        }).OrderBy(x => x.distance).FirstOrDefault();
        return candidate.distance <= SnapTolerance ? candidate.projected : point;
    }
    public void AddSegment(Point2D start, Point2D end)
    {
        if (start.DistanceTo(end) <= SnapTolerance) return;
        var startNode = GetOrCreateNode(start);
        var endNode = GetOrCreateNode(end);
        var splitPoints = new List<(double T, PipeNode Node)> { (0, startNode), (1, endNode) };

        foreach (var edge in _network.Edges.ToList())
        {
            var a = _network.FindNode(edge.StartNodeId)!;
            var b = _network.FindNode(edge.EndNodeId)!;
            var intersection = Segment2D.Intersect(startNode.Position, endNode.Position, a.Position, b.Position, SnapTolerance);
            if (intersection.Kind == SegmentIntersectionKind.Point && intersection.Point is not null)
            {
                var node = GetOrCreateNode(intersection.Point);
                splitPoints.Add((intersection.TOnFirst, node));
                SplitEdgeAt(edge, node, intersection.TOnSecond);
            }
            else if (intersection.Kind == SegmentIntersectionKind.Overlap)
            {
                // 공선 중첩은 각 선의 끝점을 서로의 분할점으로 사용해 동일 그래프로 수렴시킨다.
                AddIfOnSegment(splitPoints, a, startNode.Position, endNode.Position);
                AddIfOnSegment(splitPoints, b, startNode.Position, endNode.Position);
                SplitEdgeAt(edge, startNode, Segment2D.ParameterOnSegment(startNode.Position, a.Position, b.Position));
                SplitEdgeAt(edge, endNode, Segment2D.ParameterOnSegment(endNode.Position, a.Position, b.Position));
            }
        }

        foreach (var pair in splitPoints.OrderBy(x => x.T).DistinctBy(x => x.Node.Id).Zip(splitPoints.OrderBy(x => x.T).DistinctBy(x => x.Node.Id).Skip(1)))
            AddEdgeIfMissing(pair.First.Node.Id, pair.Second.Node.Id);
        ClassifyNodes();
    }

    public void AddInlineFitting(Guid edgeId, string typeKey)
    {
        var edge = _network.FindEdge(edgeId) ?? throw new ArgumentOutOfRangeException(nameof(edgeId));
        var order = edge.InlineFittings.Count;
        edge.AddFitting(new InlineFitting(typeKey, (order + 1d) / (order + 2d), order));
    }

    public void RemoveInlineFitting(Guid edgeId, Guid fittingId)
    {
        var edge = _network.FindEdge(edgeId) ?? throw new ArgumentOutOfRangeException(nameof(edgeId));
        var remaining = edge.InlineFittings.Where(x => x.Id != fittingId).Select((x, i) => x with { Order = i }).ToList();
        _network.RemoveEdge(edge);
        _network.AddEdge(edge.StartNodeId, edge.EndNodeId, remaining);
    }

    private PipeNode GetOrCreateNode(Point2D position) => _network.Nodes.FirstOrDefault(x => x.Position.DistanceTo(position) <= SnapTolerance) ?? _network.AddNode(position);

    private void AddIfOnSegment(List<(double T, PipeNode Node)> points, PipeNode node, Point2D start, Point2D end)
    {
        var t = Segment2D.ParameterOnSegment(node.Position, start, end);
        var projected = new Point2D(start.X + (end.X - start.X) * t, start.Y + (end.Y - start.Y) * t);
        if (t >= -SnapTolerance && t <= 1 + SnapTolerance && projected.DistanceTo(node.Position) <= SnapTolerance) points.Add((Math.Clamp(t, 0, 1), node));
    }

    private void SplitEdgeAt(PipeEdge edge, PipeNode node, double t)
    {
        if (node.Id == edge.StartNodeId || node.Id == edge.EndNodeId || t <= 0.0001 || t >= 0.9999) return;
        var first = edge.InlineFittings.Where(x => x.T < t).Select((x, i) => x with { T = x.T / t, Order = i }).ToList();
        var second = edge.InlineFittings.Where(x => x.T >= t).Select((x, i) => x with { T = (x.T - t) / (1 - t), Order = i }).ToList();
        _network.RemoveEdge(edge);
        _network.AddEdge(edge.StartNodeId, node.Id, first);
        _network.AddEdge(node.Id, edge.EndNodeId, second);
    }

    private void AddEdgeIfMissing(Guid start, Guid end)
    {
        if (start == end || _network.Edges.Any(x => (x.StartNodeId == start && x.EndNodeId == end) || (x.StartNodeId == end && x.EndNodeId == start))) return;
        _network.AddEdge(start, end);
    }

    private void ClassifyNodes()
    {
        foreach (var node in _network.Nodes)
        {
            var connected = _network.Edges.Where(x => x.StartNodeId == node.Id || x.EndNodeId == node.Id).ToList();
            node.Degree = connected.Count;
            node.NodeKind = node.Degree switch
            {
                1 => NodeKind.Cap,
                2 => IsStraight(node, connected) ? NodeKind.Inline : NodeKind.Elbow,
                3 => NodeKind.Tee,
                >= 4 => NodeKind.Cross,
                _ => NodeKind.Inline
            };
        }
    }

    private bool IsStraight(PipeNode node, IReadOnlyList<PipeEdge> edges)
    {
        var other1 = _network.FindNode(edges[0].StartNodeId == node.Id ? edges[0].EndNodeId : edges[0].StartNodeId)!;
        var other2 = _network.FindNode(edges[1].StartNodeId == node.Id ? edges[1].EndNodeId : edges[1].StartNodeId)!;
        var dot = (other1.Position.X - node.Position.X) * (other2.Position.X - node.Position.X) + (other1.Position.Y - node.Position.Y) * (other2.Position.Y - node.Position.Y);
        var lengths = node.Position.DistanceTo(other1.Position) * node.Position.DistanceTo(other2.Position);
        return lengths > 0 && Math.Acos(Math.Clamp(dot / lengths, -1, 1)) * 180 / Math.PI >= 180 - StraightAngleToleranceDegrees;
    }
}
