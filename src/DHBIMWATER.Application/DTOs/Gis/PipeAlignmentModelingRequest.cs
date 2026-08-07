using DHBIMWATER.Core.Gis;

namespace DHBIMWATER.Application.DTOs.Gis;

public enum PipeAlignmentOutputMode { DirectShape, Beam, PipingSystem }

/// <summary>입력 파일 1개와 그 파일의 필드 매핑. null 필드는 해당 값을 읽지 않는다.</summary>
public sealed record AlignmentSourceFile(string FilePath, string PipeKind, string? DiameterField, string? KindField, IReadOnlyList<string>? Layers = null);

public sealed record PipeAlignmentModelingRequest
{
    public required IReadOnlyList<AlignmentSourceFile> Files { get; init; }
    public double ReferenceX { get; init; }
    public double ReferenceY { get; init; }
    public bool ApplySharedCoordinates { get; init; }
    public ZDatum ZDatum { get; init; } = ZDatum.AsIs;
    /// <summary>TODO: ZSource 미소비 — Phase 2.</summary>
    public ZSource ZSource { get; init; } = ZSource.GeometryZ;
    public PipeAlignmentOutputMode OutputMode { get; init; } = PipeAlignmentOutputMode.DirectShape;
    public double IntervalMm { get; init; } = 6000;
    public string? BeamTypeName { get; init; }
    public string? PipingSystemTypeName { get; init; }
    public string? PipeTypeName { get; init; }
    public string? LevelName { get; init; }
    public bool AlignTangent { get; init; } = true;
    /// <summary>절점 병합 허용오차(mm). 곡관 자리를 찾을 때 진단과 같은 그래프를 재현하려면 진단에서 쓴 값과 같아야 한다.</summary>
    public double SnapToleranceMm { get; init; } = 10;
    /// <summary>곡관 치수 조회에 쓸 형식. 같은 각도라도 A형/B형은 t가 다르다.</summary>
    public BendForm Form { get; init; } = BendForm.AType;
}

/// <param name="BendCount">직관을 비워 자리를 남긴 곡관 수. 곡관 실물 배치는 아직 하지 않는다.</param>
public sealed record PipeAlignmentModelingResult(PipeAlignmentOutputMode OutputMode, int CreatedCount, int SkippedSegments, IReadOnlyList<string> Warnings, int BendCount = 0);

// Repo 인터페이스 계약을 보존하는 기존 DTO 정의다.
public sealed record PipeAlignmentCreateDefinition(IReadOnlyList<PipeAlignment> Alignments, double ReferenceX, double ReferenceY, ZDatum ZDatum);
public sealed record PipeAlignmentCreateResult(int CreatedCount, int SkippedSegments, IReadOnlyList<string> Warnings);
public sealed record AlignmentPlacementOrigin(double X, double Y, ZDatum ZDatum);
