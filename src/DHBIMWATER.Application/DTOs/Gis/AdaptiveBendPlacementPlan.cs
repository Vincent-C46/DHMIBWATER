using DHBIMWATER.Core.Gis;

namespace DHBIMWATER.Application.DTOs.Gis;

/// <summary>
/// Revit Adaptive Component 곡관 배치에 필요한 값. <see cref="BendPlacement"/>에서 카탈로그 부속 정보
/// (FamilyName/TypeName)가 확인된 것만 여기로 넘어온다.
/// </summary>
/// <param name="OuterDiameterMm">DN — 곡관 패밀리의 OD 인스턴스 매개변수에 대응.</param>
/// <param name="WallThicknessMm">e — 곡관 패밀리의 thk 인스턴스 매개변수에 대응.</param>
/// <param name="RotXYDeg">P1~P5 각 점의 rot_XY_n(도). <see cref="Points"/>의 5점 순서와 인덱스가 같다.</param>
/// <param name="RotXZDeg">P1~P5 각 점의 rot_XZ_n(도). <see cref="RotXYDeg"/>와 인덱스가 같다.</param>
public sealed record AdaptiveBendPlacementPlan(
    int NodeId,
    string FamilyName,
    string TypeName,
    BendArcPoints Points,
    double OuterDiameterMm,
    double WallThicknessMm,
    IReadOnlyList<double> RotXYDeg,
    IReadOnlyList<double> RotXZDeg);

/// <param name="Count">실제로 배치된 곡관 수.</param>
/// <param name="Warnings">OD/thk/rot_XY_/rot_XZ_ 등 패밀리 인스턴스 파라미터를 찾지 못하거나 읽기전용이라 값을 못 넣은 경우의 안내.</param>
public sealed record AdaptiveBendPlacementResult(int Count, IReadOnlyList<string> Warnings);
