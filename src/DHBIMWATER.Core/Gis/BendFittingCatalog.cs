namespace DHBIMWATER.Core.Gis;

/// <summary>적용 규격이 정의하는 이형관 형식 구분. Joint Type과는 별개다.</summary>
public enum BendForm { AType, BType }

/// <param name="DiameterMm">호칭지름 DN. 조회는 Exact Match다.</param>
/// <param name="WallThicknessMm">e — 곡관 자체의 규격상 벽두께(mm).</param>
/// <param name="TypeName">선택한 곡관 패밀리 안의 타입명.</param>
/// <param name="Material">
/// 이 규격이 속한 관종. DN·각도·형식만으로는 관종이 구분되지 않아 조회 키에 필요하다.
/// 기본값이 있으므로 관종 축 도입 이전에 저장된 JSON도 그대로 역직렬화된다.
/// </param>
public sealed record BendFittingEntry(
    double DiameterMm,
    double AngleDeg,
    BendForm Form,
    double LayingLengthMm,
    double CenterlineRadiusMm,
    double ExtraLegLengthMm = 0d,
    double WallThicknessMm = 0d,
    string? TypeName = null,
    PipeMaterial Material = PipeMaterial.DuctileIron);

public sealed class BendFittingCatalog
{
    private const double Epsilon = 1e-6;

    public BendFittingCatalog(IReadOnlyList<BendFittingEntry> entries) => Entries = entries;
    public IReadOnlyList<BendFittingEntry> Entries { get; }

    /// <summary>
    /// 임시 기본값 그대로인지. 사용자가 [기본값 복원] 후 저장하면 임시값이 프로젝트에 남는데
    /// 그때도 경고가 유지되도록 출처가 아니라 <b>내용</b>으로 판단한다.
    /// </summary>
    public bool IsPlaceholder => DefaultIsPlaceholder && Entries.SequenceEqual(Default.Entries);

    public BendFittingEntry? Find(double diameterMm, double angleDeg, BendForm form, PipeMaterial material = PipeMaterial.DuctileIron) => Entries.FirstOrDefault(x =>
        x.Material == material
        && x.Form == form
        && Math.Abs(x.DiameterMm - diameterMm) <= Epsilon
        && Math.Abs(x.AngleDeg - angleDeg) <= Epsilon);

    // ══════════════════════════════════════════════════════════════════════════
    // ⚠ 임시값 — 실제 규격 아님 (사용자 지시 2026-08-18)
    //
    // 주철관 핸드북 곡관 규격표를 아직 확보하지 못해, 판정·배치 파이프라인이 돌아가도록
    // 형상만 성립하는 값을 넣어 둔 것이다. 수량 산출·간섭 검토·도면 제출에 그대로 쓰면 안 된다.
    // 자료가 들어오면 아래 IsPlaceholder / 생성 블록을 통째로 실제 표로 교체한다.
    //
    // 생성 규칙 — 곡률반경 R = 계수 × DN, 배관길이 L = 접선길이(R·tan(θ/2)) + 여유 50mm.
    // 여유를 두는 이유: L < 접선길이면 BendResolution.IsSizeConsistent가 false가 되어
    // 전 절점이 치수 불일치로 보고된다. 임시값 때문에 그 경고가 묻히면 안 된다.
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// <see cref="Default"/>가 실제 규격이 아니라 임시값인지. 실제 표로 교체할 때 false로 바꾼다.
    /// 사용자 안내 문구를 분기하는 데 쓴다.
    /// </summary>
    public const bool DefaultIsPlaceholder = true;

    /// <summary>형식별 곡률반경 계수(R = 계수 × DN). A형을 완만하게 둔 임시 구분이다.</summary>
    private static readonly (BendForm Form, double RadiusFactor)[] PlaceholderForms =
    {
        (BendForm.AType, 2.5), (BendForm.BType, 1.5)
    };

    private const double PlaceholderLayingClearanceMm = 50d;

    private static IEnumerable<BendFittingEntry> BuildPlaceholders() =>
        from dn in StraightPipeSpecTable.NominalDiameters
        from form in PlaceholderForms
        from angle in JointDeflectionRule.StandardAngles
        let radius = Math.Round(form.RadiusFactor * dn, 1)
        select new BendFittingEntry(
            dn,
            angle,
            form.Form,
            Math.Ceiling(BendResolver.TangentLength(radius, angle)) + PlaceholderLayingClearanceMm,
            radius,
            0d,
            // 벽두께는 상수 1종관 직관 두께를 빌려 쓴다. 곡관 실제 두께와 다르다.
            StraightPipeSpecTable.Default.Find(PipeKindCatalog.Water1, dn)?.ThicknessMm ?? 0d,
            // 타입명은 문서에 로드된 곡관 패밀리에 따라 달라지므로 비워 두고 사용자가 규격표 창에서 고른다.
            TypeName: null);

    public static BendFittingCatalog Default { get; } = new(BuildPlaceholders().ToList());
}

/// <summary>관로 규격과 판정 설정의 저장 단위.</summary>
public sealed record BendSettings(
    StraightPipeSpecTable StraightPipes,
    JointDeflectionTable JointDeflections,
    BendFittingCatalog Fittings,
    string ActiveJointType,
    JointApplicationMode ApplicationMode)
{
    public static BendSettings Default { get; } = new(
        StraightPipeSpecTable.Default,
        JointDeflectionTable.Default,
        BendFittingCatalog.Default,
        JointTypeCatalog.KpMechanical,
        JointApplicationMode.SingleJoint);
}
