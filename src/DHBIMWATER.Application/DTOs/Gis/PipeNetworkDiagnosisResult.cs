using DHBIMWATER.Core.Gis;

namespace DHBIMWATER.Application.DTOs.Gis;

public sealed record PipeNetworkDiagnosisRequest
{
    public required IReadOnlyList<AlignmentPlacementFile> Files { get; init; }
    /// <summary>밀리미터 단위 UI 입력값이다. 실행 시 GIS 좌표(m) 단위로 변환한다.</summary>
    public double SnapToleranceMm { get; init; } = 10;
    public bool ParseCombinedDiameter { get; init; } = true;
    /// <summary>곡관 치수 조회에 쓸 형식. 같은 각도라도 A형/B형은 t가 다르다.</summary>
    public BendForm Form { get; init; } = BendForm.AType;
}

/// <param name="X">원본 GIS 좌표(m). Revit 내부좌표 변환은 배치 단계에서 한다.</param>
/// <param name="LayingLengthMm">t — 절점에서 곡관 끝단까지의 거리. 치수 미입력이면 0.</param>
/// <param name="CenterlineRadiusMm">R — 중심선 호의 곡률반경. 치수 미입력이면 0.</param>
/// <param name="TangentLengthMm">T = R·tan(θ/2). 계산값.</param>
/// <param name="SizeConsistent">t ≥ T 인지. false면 호가 곡관 밖으로 나가는 치수다.</param>
public sealed record PipeNetworkNodeReport(
    int NodeId, string Kind, int Degree, double X, double Y, double Z,
    double DeflectionDeg, double MaxDiameterMm, double MinDiameterMm,
    string PipeKind, string BendResolution, double StandardAngleDeg, double ResidualDeg,
    double LayingLengthMm, double CenterlineRadiusMm, double TangentLengthMm, bool SizeConsistent);

/// <param name="KindCounts">NodeKind 이름 → 개수.</param>
/// <param name="Attention">Unresolved + TooManyBranches + EndPoint + 치수 부정합 절점.</param>
/// <param name="SizeConflictCount">t &lt; T 인 곡관 수. 0보다 크면 호출부가 경고 다이얼로그를 띄운다.</param>
public sealed record PipeNetworkDiagnosisResult(
    int NodeCount, int EdgeCount,
    IReadOnlyDictionary<string, int> KindCounts,
    int BendStandardCount, int BendNoneCount, int BendUnresolvedCount,
    int SizeConflictCount,
    IReadOnlyList<PipeNetworkNodeReport> Attention,
    IReadOnlyList<string> Warnings);
