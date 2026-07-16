using DHBIMWATER.Core.Geometry;

namespace DHBIMWATER.Core.Piping;

/// <summary>Phase 2 Revit 생성 경계에서 사용할 순수 데이터 정의.</summary>
public sealed record PipeEdgeDefinition(Guid Id, Point2D Start, Point2D End, double Elevation);
public sealed record FittingPlacementDefinition(Guid Id, string TypeKey, Point2D Position, double Elevation, Guid? EdgeId = null, Guid? NodeId = null);
