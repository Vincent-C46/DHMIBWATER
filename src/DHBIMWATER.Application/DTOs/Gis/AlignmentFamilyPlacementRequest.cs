namespace DHBIMWATER.Application.DTOs.Gis;

public enum AlignmentPlacementTarget { Beam, PipingSystem }
public sealed record AlignmentPlacementFile(string FilePath, string PipeKind);

public sealed record AlignmentFamilyPlacementRequest
{
    public required IReadOnlyList<AlignmentPlacementFile> Files { get; init; }
    /// <summary>밀리미터 단위 UI 입력값이다. 실행 시 GIS 좌표(m) 단위로 변환한다.</summary>
    public double IntervalMm { get; init; } = 6000;
    public AlignmentPlacementTarget Target { get; init; } = AlignmentPlacementTarget.Beam;
    public string? BeamTypeName { get; init; }
    public string? PipingSystemTypeName { get; init; }
    public string? PipeTypeName { get; init; }
    public string? LevelName { get; init; }
    public bool AlignTangent { get; init; } = true;
    public bool ParseCombinedDiameter { get; init; } = true;
}

public sealed record AlignmentFamilyPlacementResult(int PlacedCount, IReadOnlyList<string> Warnings);
