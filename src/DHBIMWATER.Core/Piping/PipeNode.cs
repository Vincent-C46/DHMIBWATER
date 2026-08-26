using DHBIMWATER.Core.Geometry;

namespace DHBIMWATER.Core.Piping;

public sealed class PipeNode
{
    public PipeNode(Guid id, Point2D position)
    {
        Id = id;
        Position = position;
    }

    public Guid Id { get; }
    public Point2D Position { get; }
    public int Degree { get; internal set; }
    public NodeKind NodeKind { get; internal set; } = NodeKind.Inline;
}
