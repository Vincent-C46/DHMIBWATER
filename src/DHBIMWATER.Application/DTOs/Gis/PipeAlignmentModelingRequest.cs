using DHBIMWATER.Core.Gis;

namespace DHBIMWATER.Application.DTOs.Gis;

public enum PipeAlignmentOutputMode { DirectShape, Beam, PipingSystem }

/// <summary>엑셀 시트 1개를 관로 선형으로 읽기 위한 열 매핑. X·Y·Z는 필수, 나머지는 선택.</summary>
/// <param name="StationColumnIndex">선택. 체이니지 정렬 검증용.</param>
/// <param name="DiameterColumnIndex">선택. 시트 전체가 관로 1개(=레코드 1개)이므로, 데이터 영역에서 처음 찾은 값 하나를 그 레코드의 직경 필드값으로 쓴다.</param>
/// <param name="KindColumnIndex">선택. DiameterColumnIndex와 동일한 방식으로 관종 필드값을 읽는다.</param>
/// <param name="FittingColumnIndex">선택. DiameterColumnIndex와 동일한 방식으로 피팅(이형관)명을 읽는다. 배치 로직은 아직 없고 값만 속성으로 보관한다.</param>
public sealed record ExcelAlignmentMapping(string SheetName, int HeaderRow, int DataStartRow, int XColumnIndex, int YColumnIndex, int ZColumnIndex, int? StationColumnIndex, int? DiameterColumnIndex = null, int? KindColumnIndex = null, int? FittingColumnIndex = null);

/// <summary>입력 파일 1개와 그 파일의 필드 매핑. null 필드는 해당 값을 읽지 않는다. ExcelMapping이 있으면 엑셀 리더가 재읽기 시에도 이를 사용한다.</summary>
/// <param name="ManualDiameterMm">직경 필드로 해석하지 못했을 때(엑셀처럼 필드 자체가 없는 경우 포함) 대신 쓸 수동 입력값. null 또는 0 이하면 쓰지 않는다.</param>
public sealed record AlignmentSourceFile(string FilePath, string PipeKind, string? DiameterField, string? KindField, IReadOnlyList<string>? Layers = null, ExcelAlignmentMapping? ExcelMapping = null, double? ManualDiameterMm = null);

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
    /// <summary>빔 인스턴스에 직경(mm)을 기록할 파라미터명. null이면 기록하지 않는다.</summary>
    public string? BeamDiameterParameterName { get; init; }
    /// <summary>빔 인스턴스에 관종을 기록할 파라미터명. null이면 기록하지 않는다.</summary>
    public string? BeamKindParameterName { get; init; }
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
