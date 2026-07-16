using DHBIMWATER.Core.Geometry;

namespace DHBIMWATER.Core.Piping;

public enum PipeOutputMode { MepPipe, GenericModel }
public sealed record PipeNodeDefinition(Guid Id, Point2D Position, NodeKind NodeKind);
public sealed record PipeNetworkDefinition(
    IReadOnlyList<PipeNodeDefinition> Nodes,
    IReadOnlyList<PipeEdgeDefinition> Edges,
    IReadOnlyList<FittingPlacementDefinition> InlineFittings,
    double Elevation,
    double DiameterMm,
    PipeOutputMode OutputMode,
    Point2D ReferencePoint);
