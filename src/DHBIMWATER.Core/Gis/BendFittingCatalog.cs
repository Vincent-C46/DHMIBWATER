namespace DHBIMWATER.Core.Gis;

/// <summary>
/// 주철관 핸드북 곡관 치수표의 형식 구분. 같은 호칭지름·각도라도 형식에 따라 t가 다르다.
/// </summary>
public enum BendForm
{
    /// <summary>A형 — 호 양끝에 플랜지가 바로 붙는 형식.</summary>
    AType,
    /// <summary>B형 — 한쪽에 직관부(치수표의 s)가 더 붙는 형식.</summary>
    BType
}

/// <param name="PipeKind">관종. 빈 문자열은 "관종 무관"으로 본다.</param>
/// <param name="DiameterMm">직경 구간 상한("이 직경 이하"를 포괄).</param>
/// <param name="AngleDeg">표준 곡관 각도(<see cref="BendToleranceTable.StandardAngles"/> 중 하나).</param>
/// <param name="Form">곡관 형식(A형/B형).</param>
/// <param name="LayingLengthMm">
/// t — 절점(두 직관 축의 교점)에서 곡관 짧은 쪽 끝단까지의 거리(mm).
/// 호의 시작점까지의 거리(접선길이 T)가 아니다. 곡관 양끝의 직관부 때문에 보통 t &gt; T다.
/// </param>
/// <param name="CenterlineRadiusMm">R — 직선과 직선 사이에 들어가는 중심선 호의 곡률반경(mm).</param>
/// <param name="ExtraLegLengthMm">
/// s — B형에서 한쪽에만 더 붙는 직관부(mm). 긴 쪽 = t + s, 짧은 쪽 = t다. A형은 0.
/// 어느 방향이 긴 쪽인지는 치수가 아니라 배치 규칙이 정한다(사용자 결정 2026-08-07: 폴리선 진행 방향 기준 하류쪽).
/// </param>
/// <param name="WallThicknessMm">e — 곡관 벽 두께(mm). 곡관 패밀리의 thk 인스턴스 매개변수에 대응한다.</param>
/// <param name="FamilyName">3점(5점) 가변 곡관 패밀리명. null이면 실물 배치를 건너뛰고 자리만 비운다.</param>
/// <param name="TypeName">곡관 패밀리의 타입명. null이면 실물 배치를 건너뛴다.</param>
public sealed record BendFittingEntry(
    string PipeKind,
    double DiameterMm,
    double AngleDeg,
    BendForm Form,
    double LayingLengthMm,
    double CenterlineRadiusMm,
    double ExtraLegLengthMm = 0d,
    double WallThicknessMm = 0d,
    string? FamilyName = null,
    string? TypeName = null);

/// <summary>관종·직경·각도·형식별 곡관 치수 카탈로그. 근거는 주철관 핸드북 곡관 치수표다.</summary>
public sealed class BendFittingCatalog
{
    private const double AngleEpsilon = 1e-6;

    public BendFittingCatalog(IReadOnlyList<BendFittingEntry> entries) => Entries = entries;

    public IReadOnlyList<BendFittingEntry> Entries { get; }

    public static BendFittingCatalog Empty { get; } = new(Array.Empty<BendFittingEntry>());

    /// <summary>
    /// 조회 규칙: (1) 형식이 같고 각도가 일치(1e-6)하는 항목만 후보,
    /// (2) 그 안에서 관종 일치 항목을 우선하고 없으면 후보 전체,
    /// (3) DiameterMm이 입력 직경 이상인 것 중 가장 작은 직경, (4) 없으면 최대 직경 항목,
    /// (5) 후보가 하나도 없으면 null(= 치수 미입력, 차감 없음).
    /// </summary>
    public BendFittingEntry? Find(string pipeKind, double diameterMm, double angleDeg, BendForm form)
    {
        var candidates = Entries
            .Where(x => x.Form == form && Math.Abs(x.AngleDeg - angleDeg) <= AngleEpsilon)
            .ToList();
        if (candidates.Count == 0) return null;

        var sameKind = candidates.Where(x => string.Equals(x.PipeKind, pipeKind, StringComparison.OrdinalIgnoreCase)).ToList();
        var pool = sameKind.Count > 0 ? sameKind : candidates;

        var covering = pool.Where(x => x.DiameterMm >= diameterMm).OrderBy(x => x.DiameterMm).FirstOrDefault();
        return covering ?? pool.OrderByDescending(x => x.DiameterMm).FirstOrDefault();
    }
}

/// <summary>설정 저장 단위. 허용굴곡과 곡관 치수는 항상 함께 읽고 함께 쓴다.</summary>
public sealed record BendSettings(BendToleranceTable Tolerance, BendFittingCatalog Fittings)
{
    /// <summary>허용굴곡은 임시 기본값, 곡관 치수는 비어 있다(제조사 실측치라 임의값을 넣지 않는다).</summary>
    public static BendSettings Default { get; } = new(BendToleranceTable.Default, BendFittingCatalog.Empty);
}
