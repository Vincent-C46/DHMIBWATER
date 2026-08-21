using DHBIMWATER.Core.Geometry;

namespace DHBIMWATER.Core.Piping;

/// <summary>모델 단위(mm)의 정규화된 배관 토폴로지 그래프.</summary>
public sealed class PipeNetwork
{
    private readonly List<PipeNode> _nodes = [];
    private readonly List<PipeEdge> _edges = [];

    public PipeNetwork(double elevation = 0) => Elevation = elevation;

    /// <summary>Phase 2에서 사용자 입력으로 연결할 작업 표고(mm).</summary>
    public double Elevation { get; set; }
    public IReadOnlyList<PipeNode> Nodes => _nodes;
    public IReadOnlyList<PipeEdge> Edges => _edges;

    public Point2D? FindSnapPoint(Point2D point, PipeSnapMode modes) => new PipeTopologyBuilder(this).FindSnapPoint(point, modes);
    public void AddSegment(Point2D start, Point2D end) => new PipeTopologyBuilder(this).AddSegment(start, end);
    public void RemoveSegment(Guid edgeId) => new PipeTopologyBuilder(this).RemoveSegment(edgeId);
    public void AddInlineFitting(Guid edgeId, string typeKey, string familyTypeName, double desiredT) => new PipeTopologyBuilder(this).AddInlineFitting(edgeId, typeKey, familyTypeName, desiredT);
    public void RemoveInlineFitting(Guid edgeId, Guid fittingId) => new PipeTopologyBuilder(this).RemoveInlineFitting(edgeId, fittingId);
    public void Clear() { _nodes.Clear(); _edges.Clear(); }
    public PipeNetwork DeepClone()
    {
        var clone = new PipeNetwork(Elevation);
        clone._nodes.AddRange(_nodes.Select(x => new PipeNode(x.Id, x.Position) { Degree = x.Degree, NodeKind = x.NodeKind }));
        clone._edges.AddRange(_edges.Select(x => new PipeEdge(x.Id, x.StartNodeId, x.EndNodeId, x.InlineFittings)));
        return clone;
    }

    public PipeNetworkDefinition ToDefinition(
        double diameterMm,
        PipeOutputMode outputMode,
        Point2D referencePoint,
        string pipingSystemTypeName = "",
        string pipeTypeName = "",
        string levelName = "",
        PipeSegmentFamilySelection? segmentFamilies = null)
    {
        var nodes = _nodes.Select(x => new PipeNodeDefinition(x.Id, x.Position, x.NodeKind)).ToList();
        var edges = _edges.Select(x =>
        {
            var start = FindNode(x.StartNodeId)!;
            var end = FindNode(x.EndNodeId)!;
            return new PipeEdgeDefinition(x.Id, start.Position, end.Position, Elevation);
        }).ToList();
        var fittings = _edges.SelectMany(edge => edge.InlineFittings.Select(fitting =>
        {
            var start = FindNode(edge.StartNodeId)!;
            var end = FindNode(edge.EndNodeId)!;
            var point = new Point2D(start.Position.X + (end.Position.X - start.Position.X) * fitting.T,
                                    start.Position.Y + (end.Position.Y - start.Position.Y) * fitting.T);
            return new FittingPlacementDefinition(fitting.Id, fitting.TypeKey, fitting.FamilyTypeName, point, Elevation, edge.Id);
        })).ToList();
        return new PipeNetworkDefinition(nodes, edges, fittings, Elevation, diameterMm, outputMode, referencePoint,
            pipingSystemTypeName, pipeTypeName, levelName, segmentFamilies);
    }

    internal PipeNode AddNode(Point2D position) { var node = new PipeNode(Guid.NewGuid(), position); _nodes.Add(node); return node; }
    internal void AddEdge(Guid start, Guid end, IEnumerable<InlineFitting>? fittings = null) => _edges.Add(new PipeEdge(Guid.NewGuid(), start, end, fittings));
    internal void RemoveEdge(PipeEdge edge) => _edges.Remove(edge);
    internal void RemoveUnconnectedNodes() => _nodes.RemoveAll(node => _edges.All(edge => edge.StartNodeId != node.Id && edge.EndNodeId != node.Id));
    internal PipeNode? FindNode(Guid id) => _nodes.FirstOrDefault(x => x.Id == id);
    internal PipeEdge? FindEdge(Guid id) => _edges.FirstOrDefault(x => x.Id == id);
}
