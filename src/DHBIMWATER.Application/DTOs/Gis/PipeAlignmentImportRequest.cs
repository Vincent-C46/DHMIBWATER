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
    /// <summary>true면 기준점을 프로젝트 공유좌표(SetInternalOriginSharedPosition)에도 기록한다. 기본값 false — 여러 SHP를 순차 임포트할 때 공유좌표가 매번 갱신되는 것을 방지.</summary>
    public bool ApplySharedCoordinates { get; init; } = false;
}

public sealed record PipeAlignmentCreateDefinition(
    IReadOnlyList<PipeAlignment> Alignments,
    double ReferenceX,
    double ReferenceY,
    double ReferenceZ,
    ZDatum ZDatum);

public sealed record PipeAlignmentCreateResult(int CreatedCount, int SkippedSegments, IReadOnlyList<string> Warnings);
