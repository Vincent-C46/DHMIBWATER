namespace DHBIMWATER.Core.Gis;

/// <summary>관종·직경 조합의 허용굴곡(도). DiameterMm은 "이 직경 이하"를 포괄하는 구간 상한이다.</summary>
public sealed record BendToleranceEntry(string PipeKind, double DiameterMm, double ToleranceDeg);

/// <summary>편각이 표준 곡관 각도에 얼마나 가까워야 그 곡관으로 보는지를 담은 표. Revit 의존 없는 순수 계산이다.</summary>
public sealed class BendToleranceTable
{
    /// <summary>표준 곡관 각도(도). 이 목록 자체는 사용자 설정 대상이 아니다.</summary>
    public static readonly IReadOnlyList<double> StandardAngles = new[] { 11.25, 22.5, 45d, 90d };

    public BendToleranceTable(IReadOnlyList<BendToleranceEntry> entries) => Entries = entries;

    public IReadOnlyList<BendToleranceEntry> Entries { get; }

    /// <summary>
    /// 관종·직경에 해당하는 허용굴곡(도). 조회 규칙:
    /// (1) 관종이 같은 항목 중 DiameterMm이 입력 직경 이상인 것 가운데 가장 작은 직경의 값,
    /// (2) 그런 항목이 없으면 같은 관종의 최대 직경 항목 값,
    /// (3) 관종이 하나도 없으면 관종 무관 전체에 같은 규칙 적용,
    /// (4) Entries가 비어 있으면 0을 반환한다(=곡관을 하나도 못 넣고 전부 Unresolved가 되어 설정 누락이 드러난다).
    /// </summary>
    public double ToleranceFor(string pipeKind, double diameterMm)
    {
        if (Entries.Count == 0) return 0d;

        var sameKind = Entries.Where(x => string.Equals(x.PipeKind, pipeKind, StringComparison.OrdinalIgnoreCase)).ToList();
        return Pick(sameKind.Count > 0 ? sameKind : Entries, diameterMm)?.ToleranceDeg ?? 0d;
    }

    /// <summary>직경 구간 상한 규칙으로 한 항목을 고른다. 입력 직경 이상 중 최소, 없으면 최대 직경 항목.</summary>
    private static BendToleranceEntry? Pick(IReadOnlyList<BendToleranceEntry> candidates, double diameterMm)
    {
        var covering = candidates.Where(x => x.DiameterMm >= diameterMm).OrderBy(x => x.DiameterMm).FirstOrDefault();
        return covering ?? candidates.OrderByDescending(x => x.DiameterMm).FirstOrDefault();
    }

    /// <summary>설정 미저장 상태에서 UI가 초기값으로 제시하는 임시 표.</summary>
    // TODO: KWWA/KS 표로 검증 필요 — 사용자 확인 전 임시값
    public static BendToleranceTable Default { get; } = new(new[]
    {
        new BendToleranceEntry(string.Empty, 450d, 5d),
        new BendToleranceEntry(string.Empty, 800d, 4d),
        new BendToleranceEntry(string.Empty, 1500d, 3d)
    });
}
