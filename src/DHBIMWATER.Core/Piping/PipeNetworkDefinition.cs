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

/// <summary>관·절점부속 배치(<see cref="PipeOutputMode.PipeAccessorySegment"/>)에 필요한 패밀리·파라미터 지정 묶음.
/// 이름은 모두 "패밀리명 : 타입명" 형식이다.</summary>
public sealed record PipeSegmentFamilySelection(
    /// <summary>직관·단관을 모두 만드는 단일 관 패밀리. 길이는 <see cref="LengthParameterName"/>으로 구동한다.</summary>
    string SegmentFamilyTypeName,
    /// <summary>관 인스턴스에 실제 길이(mm)를 기록할 파라미터명.</summary>
    string LengthParameterName,
    /// <summary>각도 구분 없이 모든 곡관 절점에 배치하는 단일 곡관 패밀리.</summary>
    string BendFamilyTypeName,
    string TeeFamilyTypeName,
    /// <summary>직관 1본의 정척 길이(mm). 패밀리 수식의 직관/단관 분기 기준과 같아야 한다.</summary>
    double StraightLengthMm = PipeSegmentPlan.StraightLengthMm,
    /// <summary>관 인스턴스에 직경(mm)을 기록할 파라미터명. 비어 있으면 직경을 구동하지 않는다.</summary>
    string DiameterParameterName = "");

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
