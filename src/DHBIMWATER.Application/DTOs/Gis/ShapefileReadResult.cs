using DHBIMWATER.Core.Gis;

namespace DHBIMWATER.Application.DTOs.Gis;

public sealed record ShapefileFieldInfo(string Name, char Type, int Length, int DecimalCount);
public sealed record ShapefileExtent(double XMin, double YMin, double XMax, double YMax, double ZMin, double ZMax)
{
    public static readonly ShapefileExtent Empty = new(0, 0, 0, 0, 0, 0);
}

public sealed record ShapefileReadResult(
    IReadOnlyList<PipeAlignment> Features,
    IReadOnlyList<ShapefileFieldInfo> Fields,
    ShapefileExtent Extent,
    string? ProjectionWkt,
    string? RecommendedEpsg,
    string EncodingName,
    int RecordCount,
    int VertexCount,
    int SegmentCount,
    IReadOnlyList<string> Warnings,
    IReadOnlyDictionary<string, string>? SampleAttributes);
