using DHBIMWATER.Core.Geometry;

namespace DHBIMWATER.Core.Piping;

public enum PipeOutputMode
{
    MepPipe,
    GenericModel,
    /// <summary>배관 밸브류 카테고리의 직관·단관 패밀리로 관을 만든다(docs/39). MEP Pipe를 만들지 않는다.</summary>
    PipeAccessorySegment
}
public sealed record PipeNodeDefinition(Guid Id, Point2D Position, NodeKind NodeKind);

/// <summary>직관·단관 배치(<see cref="PipeOutputMode.PipeAccessorySegment"/>)에 필요한 패밀리·파라미터 지정 묶음.
/// 이름은 모두 "패밀리명 : 타입명" 형식이다.</summary>
public sealed record PipeSegmentFamilySelection(
    string StraightFamilyTypeName,
    string ShortFamilyTypeName,
    /// <summary>단관 인스턴스에 실제 길이(mm)를 기록할 파라미터명.</summary>
    string ShortLengthParameterName,
    string Bend90FamilyTypeName,
    string Bend45FamilyTypeName,
    string TeeFamilyTypeName,
    double StraightLengthMm = PipeSegmentPlan.StraightLengthMm);

public sealed record PipeNetworkDefinition(
    IReadOnlyList<PipeNodeDefinition> Nodes,
    IReadOnlyList<PipeEdgeDefinition> Edges,
    IReadOnlyList<FittingPlacementDefinition> InlineFittings,
    double Elevation,
    double DiameterMm,
    PipeOutputMode OutputMode,
    Point2D ReferencePoint,
    string PipingSystemTypeName = "",
    string PipeTypeName = "",
    string LevelName = "",
    PipeSegmentFamilySelection? SegmentFamilies = null);
