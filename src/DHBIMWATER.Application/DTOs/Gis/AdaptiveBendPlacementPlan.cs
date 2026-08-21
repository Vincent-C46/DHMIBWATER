using DHBIMWATER.Core.Gis;

namespace DHBIMWATER.Application.DTOs.Gis;

/// <summary>
/// Revit Adaptive Component 곡관 배치에 필요한 값. <see cref="BendPlacement"/>에서 카탈로그 부속 정보
/// (FamilyName/TypeName)가 확인된 것만 여기로 넘어온다.
/// </summary>
/// <param name="DiameterMm">호칭지름 DN.</param>
/// <param name="WallThicknessEMm">곡관 치수표의 벽두께 e.</param>
/// <param name="ZOffsetM">인접 직관과 동일한 중심선 Z 환산 오프셋(m).</param>
/// <param name="OuterDiameterParameterName">외경 OD를 기록할 인스턴스 파라미터명. 곡관 형상 구동값이다. null이면 기록하지 않는다.</param>
/// <param name="RotXYDeg">P1~P5 각 점의 rot_XY_n(도). <see cref="Points"/>의 5점 순서와 인덱스가 같다.</param>
/// <param name="RotXZDeg">P1~P5 각 점의 rot_XZ_n(도). <see cref="RotXYDeg"/>와 인덱스가 같다.</param>
public sealed record AdaptiveBendPlacementPlan(
    int NodeId,
    string FamilyName,
    string TypeName,
    BendArcPoints Points,
    double DiameterMm,
    double WallThicknessEMm,
    double ZOffsetM,
    string? DiameterParameterName,
    string? WallThicknessParameterName,
    string? OuterDiameterParameterName,
    bool IsAcceptable,
    IReadOnlyList<double> RotXYDeg,
    IReadOnlyList<double> RotXZDeg,
    string PipeKind = "",
    string SourceFile = "",
    string RecordNumber = "",
    double? OuterDiameterMm = null,
    double DeflectionDeg = 0d,
    double StandardAngleDeg = 0d,
    double EffectiveAllowableDeg = 0d,
    double ResidualDeg = 0d,
    double CenterlineRadiusMm = 0d,
    double LayingLengthMm = 0d,
    BendForm Form = BendForm.BType,
    double WeightKg = 0d);

/// <param name="Count">실제로 배치된 곡관 수.</param>
/// <param name="Warnings">OD/thk/rot_XY_/rot_XZ_ 등 패밀리 인스턴스 파라미터를 찾지 못하거나 읽기전용이라 값을 못 넣은 경우의 안내.</param>
public sealed record AdaptiveBendPlacementResult(
    int Count,
    IReadOnlyList<string> Warnings,
    IReadOnlyDictionary<(string PipeKind, double DiameterMm), double>? OuterDiametersMm = null);
