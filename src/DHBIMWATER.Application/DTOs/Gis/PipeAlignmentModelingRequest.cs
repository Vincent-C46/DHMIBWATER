using DHBIMWATER.Core.Gis;

namespace DHBIMWATER.Application.DTOs.Gis;

/// <summary>Adaptive — 직관 2점 가변 + 곡관 5점 가변. (구 Beam: 구조 프레이밍 배치, 2026-08-18 교체)</summary>
public enum PipeAlignmentOutputMode { DirectShape, Adaptive, PipingSystem }

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
    /// <summary>
    /// 평면 기준점(원본 좌표계, m). 이 점이 프로젝트 기준점(PBP)에 놓이도록 전체 형상을 평행이동한다.
    /// null이면 첫 유효 정점에서 자동 산출한다. (0, 0)은 "이동하지 않음"(원본 좌표 그대로 배치)을 뜻하며,
    /// 자동 산출로 대체되지 않는다 — 호출부가 좌표 이동 여부를 명시적으로 결정할 수 있어야 하기 때문이다.
    /// </summary>
    public double? ReferenceX { get; init; }
    public double? ReferenceY { get; init; }
    /// <summary>기준점을 프로젝트 공유좌표에도 기록할지 여부. 형상 배치 위치는 ReferenceX·Y가 결정하며 이 값과 무관하다.</summary>
    public bool ApplySharedCoordinates { get; init; }
    public ZDatum ZDatum { get; init; } = ZDatum.Invert;
    /// <summary>TODO: ZSource 미소비 — Phase 2.</summary>
    public ZSource ZSource { get; init; } = ZSource.GeometryZ;
    public PipeAlignmentOutputMode OutputMode { get; init; } = PipeAlignmentOutputMode.DirectShape;
    public double IntervalMm { get; init; } = 6000;
    /// <summary>직관 2점 가변 패밀리·타입("패밀리명 : 타입명").</summary>
    public string? StraightFamilyTypeName { get; init; }
    /// <summary>직관 인스턴스에 호칭지름(mm)을 기록할 파라미터명. null이면 기록하지 않는다.</summary>
    public string? StraightDiameterParameterName { get; init; }
    /// <summary>직관 인스턴스에 관종을 기록할 파라미터명. null이면 기록하지 않는다.</summary>
    public string? StraightKindParameterName { get; init; }
    /// <summary>직관 제원표의 외경 OD를 기록할 파라미터명. null이면 기록하지 않는다.</summary>
    public string? StraightOuterDiameterParameterName { get; init; }
    /// <summary>직관 제원표의 두께를 기록할 파라미터명. null이면 기록하지 않는다.</summary>
    public string? StraightThicknessParameterName { get; init; }
    /// <summary>5점 가변 곡관 패밀리명. 유형은 곡관 치수표 행에서 선택한다.</summary>
    public string? BendFamilyName { get; init; }
    public string? BendDiameterParameterName { get; init; }
    public string? BendWallThicknessParameterName { get; init; }
    /// <summary>곡관 인스턴스에 외경 OD(mm)를 기록할 파라미터명. 곡관 형상은 OD·thk로 구동되므로 사실상 필수다. null이면 기록하지 않는다.</summary>
    public string? BendOuterDiameterParameterName { get; init; }
    /// <summary>
    /// 규격표에 곡관 유형이 지정되지 않은 규격에 대신 쓸 기본 유형명.
    /// 곡관 형상은 유형이 아니라 OD·thk 인스턴스 파라미터가 결정하므로, 유형은 FamilySymbol을 고르는 용도일 뿐이다.
    /// </summary>
    public string? BendDefaultTypeName { get; init; }
    public string? PipingSystemTypeName { get; init; }
    public string? PipeTypeName { get; init; }
    public string? LevelName { get; init; }
    /// <summary>절점 병합 허용오차(mm). 곡관 자리를 찾을 때 진단과 같은 그래프를 재현하려면 진단에서 쓴 값과 같아야 한다.</summary>
    public double SnapToleranceMm { get; init; } = 10;
    /// <summary>곡관 치수 조회에 쓸 형식. 같은 각도라도 A형/B형은 t가 다르다.</summary>
    public BendForm Form { get; init; } = BendForm.AType;
    /// <summary>
    /// 모델링 창에서 현재 편집 중인 관·곡관 설정. 저장 여부와 관계없이 같은 화면에서 실행한 모델링에는 이 값을 사용한다.
    /// null은 UI 외 호출부와의 호환용이며, 이때 UseCase가 저장소 설정을 읽는다.
    /// </summary>
    public BendSettings? CurrentBendSettings { get; init; }
}

/// <param name="BendCount">실제로 생성되어 커밋된 5점 가변 곡관 수.</param>
public sealed record PipeAlignmentModelingResult(PipeAlignmentOutputMode OutputMode, int CreatedCount, int SkippedSegments, IReadOnlyList<string> Warnings, int BendCount = 0);

/// <summary>배치되는 관로 요소에 기록할 DH_* 정보 매개변수 중 관로 전체가 공유하는 값.</summary>
public sealed record PipeInfoParameterContext(string JointType, JointApplicationMode ApplicationMode, ZDatum ZDatum, BendForm Form, bool Enabled = true);

// Repo 인터페이스 계약을 보존하는 기존 DTO 정의다.
public sealed record PipeAlignmentCreateDefinition(IReadOnlyList<PipeAlignment> Alignments, AlignmentPlacementOrigin Origin);
public sealed record PipeAlignmentCreateResult(int CreatedCount, int SkippedSegments, IReadOnlyList<string> Warnings);
public sealed record AlignmentPlacementOrigin(double X, double Y, ZDatum ZDatum, StraightPipeSpecTable StraightPipeSpecs)
{
    /// <summary>선형별 직관 제원으로 계산한 중심선 Z 오프셋(m). 모든 배치 경로가 이 값을 공유한다.</summary>
    public double GetZOffsetM(PipeAlignment alignment) => GetZOffsetM(alignment.PipeKind, alignment.DiameterMm);

    public double GetZOffsetM(string pipeKind, double diameterMm)
        => PipeElevationDatum.OffsetMm(ZDatum, StraightPipeSpecs.Find(pipeKind, diameterMm), diameterMm) / 1000d;
}

/// <param name="Count">실제로 배치된 직관 수.</param>
/// <param name="Warnings">직관 규격 또는 매핑 파라미터를 적용하지 못한 경우의 안내.</param>
public sealed record AlignmentStraightPlacementResult(int Count, IReadOnlyList<string> Warnings);
