using DHBIMWATER.Core.Geometry;

namespace DHBIMWATER.Core.Piping;

[Flags]
public enum PipeSnapMode { None = 0, Endpoint = 1, Midpoint = 2, Quadrant = 4, Nearest = 8, Intersection = 16 }

public sealed class PipeTopologyBuilder
{
    public const double SnapTolerance = 100.0;
    private const double GeometryTolerance = 1e-6;
    private const double StraightAngleToleranceDegrees = 2.0;
    private readonly PipeNetwork _network;

    public PipeTopologyBuilder(PipeNetwork network) => _network = network;

    public Point2D? FindSnapPoint(Point2D point, PipeSnapMode modes)
    {
        var candidates = new List<(Point2D Point, double Distance)>();
        if (modes.HasFlag(PipeSnapMode.Endpoint))
            candidates.AddRange(_network.Nodes.Select(x => (x.Position, x.Position.DistanceTo(point))));
        if (modes.HasFlag(PipeSnapMode.Intersection))
            candidates.AddRange(_network.Nodes.Where(x => x.Degree >= 3).Select(x => (x.Position, x.Position.DistanceTo(point))));
        foreach (var edge in _network.Edges)
        {
            var start = _network.FindNode(edge.StartNodeId)!.Position;
            var end = _network.FindNode(edge.EndNodeId)!.Position;
            if (modes.HasFlag(PipeSnapMode.Midpoint)) AddCandidate(candidates, point, start, end, 0.5);
            if (modes.HasFlag(PipeSnapMode.Quadrant)) { AddCandidate(candidates, point, start, end, 0.25); AddCandidate(candidates, point, start, end, 0.75); }
            if (modes.HasFlag(PipeSnapMode.Nearest))
            {
                var t = Math.Clamp(Segment2D.ParameterOnSegment(point, start, end), 0, 1);
                AddCandidate(candidates, point, start, end, t);
            }
        }
        var candidate = candidates.OrderBy(x => x.Distance).FirstOrDefault();
        return candidate.Distance <= SnapTolerance ? candidate.Point : null;
    }

    private static void AddCandidate(List<(Point2D Point, double Distance)> candidates, Point2D target, Point2D start, Point2D end, double t)
    {
        var candidate = new Point2D(start.X + (end.X - start.X) * t, start.Y + (end.Y - start.Y) * t);
        candidates.Add((candidate, candidate.DistanceTo(target)));
    }
    public void AddSegment(Point2D start, Point2D end)
    {
        if (start.DistanceTo(end) <= SnapTolerance) return;
        // UI에서 선택된 OSNAP 좌표는 이미 정확한 기존 좌표다. 여기서 다시 100mm 범위의 노드로
        // 끌어당기면 다른 스냅 후보를 선택했거나 스냅을 끈 경우에도 선이 임의로 변형될 수 있다.
        var startNode = GetOrCreateNode(start, GeometryTolerance);
        var endNode = GetOrCreateNode(end, GeometryTolerance);
        var splitPoints = new List<(double T, PipeNode Node)> { (0, startNode), (1, endNode) };

        foreach (var edge in _network.Edges.ToList())
        {
            var a = _network.FindNode(edge.StartNodeId)!;
            var b = _network.FindNode(edge.EndNodeId)!;
            var intersection = Segment2D.Intersect(startNode.Position, endNode.Position, a.Position, b.Position, GeometryTolerance);
            if (intersection.Kind == SegmentIntersectionKind.Point && intersection.Point is not null)
            {
                var node = GetOrCreateNode(intersection.Point, GeometryTolerance);
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

    public void AddInlineFitting(Guid edgeId, string typeKey, string familyTypeName, double desiredT)
    {
        var edge = _network.FindEdge(edgeId) ?? throw new ArgumentOutOfRangeException(nameof(edgeId));
        var start = _network.FindNode(edge.StartNodeId)!.Position;
        var end = _network.FindNode(edge.EndNodeId)!.Position;
        var length = start.DistanceTo(end);
        var minSpacingT = Math.Min(0.45, 100 / length);
        var t = FindAvailableFittingT(edge.InlineFittings, desiredT, minSpacingT);
        edge.AddFitting(new InlineFitting(typeKey, familyTypeName, t, edge.InlineFittings.Count));
    }

    private static double FindAvailableFittingT(IReadOnlyList<InlineFitting> fittings, double desiredT, double minSpacingT)
    {
        var lower = minSpacingT;
        var upper = 1 - minSpacingT;
        for (var offset = 0; offset <= 100; offset++)
        {
            foreach (var direction in offset == 0 ? new[] { 0 } : new[] { -1, 1 })
            {
                var candidate = Math.Clamp(desiredT + direction * offset * minSpacingT, lower, upper);
                if (fittings.All(x => Math.Abs(x.T - candidate) >= minSpacingT)) return candidate;
            }
        }
        throw new InvalidOperationException("선 위에 부속품을 배치할 공간이 부족합니다.");
    }

    public void RemoveInlineFitting(Guid edgeId, Guid fittingId)
    {
        var edge = _network.FindEdge(edgeId) ?? throw new ArgumentOutOfRangeException(nameof(edgeId));
        var remaining = edge.InlineFittings.Where(x => x.Id != fittingId).Select((x, i) => x with { Order = i }).ToList();
        _network.RemoveEdge(edge);
        _network.AddEdge(edge.StartNodeId, edge.EndNodeId, remaining);
    }

    public void RemoveSegment(Guid edgeId)
    {
        var edge = _network.FindEdge(edgeId) ?? throw new ArgumentOutOfRangeException(nameof(edgeId));
        _network.RemoveEdge(edge);
        _network.RemoveUnconnectedNodes();
        ClassifyNodes();
    }

    private PipeNode GetOrCreateNode(Point2D position, double tolerance) => _network.Nodes.FirstOrDefault(x => x.Position.DistanceTo(position) <= tolerance) ?? _network.AddNode(position);

    private void AddIfOnSegment(List<(double T, PipeNode Node)> points, PipeNode node, Point2D start, Point2D end)
    {
        var t = Segment2D.ParameterOnSegment(node.Position, start, end);
        var projected = new Point2D(start.X + (end.X - start.X) * t, start.Y + (end.Y - start.Y) * t);
        var length = start.DistanceTo(end);
        var parameterTolerance = length <= double.Epsilon ? 0 : GeometryTolerance / length;
        if (t >= -parameterTolerance && t <= 1 + parameterTolerance && projected.DistanceTo(node.Position) <= GeometryTolerance)
            points.Add((Math.Clamp(t, 0, 1), node));
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
