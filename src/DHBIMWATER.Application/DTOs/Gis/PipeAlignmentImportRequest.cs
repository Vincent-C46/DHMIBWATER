using DHBIMWATER.Core.Gis;

namespace DHBIMWATER.Application.DTOs.Gis;

public sealed record PipeAlignmentImportFile(string ShpPath, string PipeKind);

public sealed record PipeAlignmentImportRequest
{
    public required IReadOnlyList<PipeAlignmentImportFile> Files { get; init; }
    public double ReferenceX { get; init; }
    public double ReferenceY { get; init; }
    public double ReferenceZ { get; init; }
    public ZSource ZSource { get; init; } = ZSource.GeometryZ;
    public ZDatum ZDatum { get; init; } = ZDatum.AsIs;
    public bool ParseCombinedDiameter { get; init; } = true;
}

public sealed record PipeAlignmentCreateDefinition(
    IReadOnlyList<PipeAlignment> Alignments,
    double ReferenceX,
    double ReferenceY,
    double ReferenceZ,
    ZDatum ZDatum);

public sealed record PipeAlignmentCreateResult(int CreatedCount, int SkippedSegments, IReadOnlyList<string> Warnings);
