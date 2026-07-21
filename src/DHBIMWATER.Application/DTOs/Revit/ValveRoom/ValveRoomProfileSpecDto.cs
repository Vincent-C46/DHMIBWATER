namespace DHBIMWATER.Application.DTOs.Revit.ValveRoom;

/// <summary>공통 단면 두께 · 높이 (3개 타입 공통).</summary>
public record ValveRoomProfileSpecDto
(
    double InnerHeight,
    double PlainConcreteThickness,
    double FoundationThickness,
    double FoundationToe,
    double OuterWallThickness,
    double UpperSlabThickness
);
