using DHBIMWATER.Core.Geometry;

namespace DHBIMWATER.Application.DTOs.Revit.ValveRoom;

/// <summary><c>void_면기반</c> 패밀리의 공기밸브실 독립기초 배치 정의. 모든 길이 치수는 mm다.</summary>
public record AirValveVoidPlacementDefinition(Point3D PipeCenter, double Radius, string Axis);
