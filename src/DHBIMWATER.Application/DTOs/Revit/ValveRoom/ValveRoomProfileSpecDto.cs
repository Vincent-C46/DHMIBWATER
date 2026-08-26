namespace DHBIMWATER.Application.DTOs.Revit.ValveRoom;

/// <summary>공통 단면 두께 · 높이 (3개 타입 공통). InnerHeight는 기초 상부면~상부슬래브 하부면의 내부 유효높이이다.</summary>
public record ValveRoomProfileSpecDto
(
    double InnerHeight,
    double PlainConcreteThickness,
    double FoundationThickness,
    double FoundationToe,
    double OuterWallThickness,
    double UpperSlabThickness
);
