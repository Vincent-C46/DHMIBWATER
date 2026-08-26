namespace DHBIMWATER.Application.DTOs.Revit.ValveRoom;

/// <summary>공기밸브실 본관 관통 Void 입력. 모든 길이 치수는 mm다.</summary>
public record AirValveRoomSpecDto(double MainPipeDiameter, double FoundationTopToPipeCenterDepth, string VoidAxis);
