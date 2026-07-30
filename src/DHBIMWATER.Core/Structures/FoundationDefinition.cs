using DHBIMWATER.Core.Geometry;

namespace DHBIMWATER.Core.Structures;

/// <summary>독립기초를 배치할 때 필요한 형상·위치 정의이다. 모든 치수는 mm다.</summary>
public record FoundationDefinition
{
    public Point3D Position { get; init; } = new(0, 0, 0);
    public double Length { get; init; }
    public double Width { get; init; }
    public double Thickness { get; init; }
    public string ElementCode { get; init; } = string.Empty;
    public string Zone { get; init; } = string.Empty;
    public string Part { get; init; } = string.Empty;
}
