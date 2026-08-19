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
    /// 저장된 값이 핸드북 반영 이전의 임시 곡관표(<see cref="LegacyPlaceholder"/>)와 같은지.
    /// 임시값이 이미 프로젝트·마스터에 저장된 상태에서 기본값만 실제 규격으로 교체됐으므로,
    /// 그런 프로젝트에는 [기본값 복원]으로 갱신하라고 안내해야 한다.
    /// </summary>
    public bool IsLegacyPlaceholder => Entries.SequenceEqual(LegacyPlaceholder.Entries);

    public BendFittingEntry? Find(double diameterMm, double angleDeg, BendForm form, PipeMaterial material = PipeMaterial.DuctileIron) => Entries.FirstOrDefault(x =>
        x.Material == material
        && x.Form == form
        && Math.Abs(x.DiameterMm - diameterMm) <= Epsilon
        && Math.Abs(x.AngleDeg - angleDeg) <= Epsilon);

    // ══════════════════════════════════════════════════════════════════════════
    // 주철관 핸드북(2020년판) 소켓곡관 규격 — 출처: docs/주철관핸드북(2020년판)_이형관.pdf
    //   Ⅴ장 4. 90° / 5. 45° / 6. 22½° / 7. 11¼° 소켓곡관 표 (DN80~1200)
    //
    // 열 대응
    //   e → WallThicknessMm, R → CenterlineRadiusMm, t → LayingLengthMm(절점→관 끝), s → ExtraLegLengthMm
    //
    // 판단 근거 (2026-08-19 사용자 확인)
    //   · A형(양쪽 소켓)과 B형(소켓+스피것)은 치수 e·R·t·s가 동일하고 무게만 다르다 → 같은 값을 두 형식에 전개한다.
    //   · s=200은 도면상 B형 스피것 쪽에만 표기된다 → A형 0, B형 200.
    //   · 표에 DE(외경) 열이 없다 → 곡관 외경은 직관 제원표(StraightPipeSpecTable)의 DE를 쓴다. DE는 관종 무관 DN 단일값이다.
    //   · 이형관 벽두께 e는 관종과 무관한 단일값이며 4개 각도가 모두 같다. 직관 두께(관종별)와는 다른 계열이다.
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>이형관 벽두께 e — DN만의 함수다(각도·관종 무관).</summary>
    private static readonly (double Dn, double WallThicknessMm)[] WallThickness =
    {
        (80, 7.0), (100, 7.2), (125, 7.5), (150, 7.8), (200, 8.4), (250, 9.0),
        (300, 9.6), (350, 10.2), (400, 10.8), (450, 11.4), (500, 12.0), (600, 13.2),
        (700, 14.4), (800, 15.6), (900, 16.8), (1000, 18.0), (1100, 19.2), (1200, 20.4)
    };

    /// <summary>B형 스피것 삽입부 s. 표 전 구간 200mm 고정이다.</summary>
    private const double SpigotLengthMm = 200d;

    /// <summary>11¼° 소켓곡관 — (DN, R, t).</summary>
    private static readonly (double Dn, double RadiusMm, double LayingMm)[] Bend1125 =
    {
        (80, 75, 55), (100, 115, 55), (125, 125, 60), (150, 150, 60), (200, 185, 65), (250, 230, 75),
        (300, 310, 80), (350, 345, 85), (400, 380, 90), (450, 415, 95), (500, 495, 100), (600, 570, 110),
        (700, 685, 120), (800, 760, 135), (900, 875, 145), (1000, 995, 155), (1100, 1075, 165), (1200, 1195, 175)
    };

    /// <summary>22½° 소켓곡관 — (DN, R, t).</summary>
    private static readonly (double Dn, double RadiusMm, double LayingMm)[] Bend225 =
    {
        (80, 85, 65), (100, 105, 65), (125, 130, 75), (150, 155, 80), (200, 195, 90), (250, 240, 100),
        (300, 300, 110), (350, 345, 120), (400, 390, 135), (450, 435, 145), (500, 495, 155), (600, 590, 175),
        (700, 695, 200), (800, 800, 220), (900, 890, 245), (1000, 995, 265), (1100, 1050, 295), (1200, 1150, 310)
    };

    /// <summary>45° 소켓곡관 — (DN, R, t).</summary>
    private static readonly (double Dn, double RadiusMm, double LayingMm)[] Bend45 =
    {
        (80, 88, 80), (100, 100, 90), (125, 120, 100), (150, 145, 110), (200, 200, 135), (250, 245, 155),
        (300, 300, 175), (350, 350, 200), (400, 400, 220), (450, 450, 245), (500, 495, 265), (600, 595, 310),
        (700, 695, 355), (800, 795, 395), (900, 895, 440), (1000, 995, 485), (1100, 1095, 525), (1200, 1195, 575)
    };

    /// <summary>90° 소켓곡관 — (DN, R, t).</summary>
    private static readonly (double Dn, double RadiusMm, double LayingMm)[] Bend90 =
    {
        (80, 75, 150), (100, 95, 170), (125, 120, 195), (150, 145, 220), (200, 195, 270), (250, 240, 320),
        (300, 290, 370), (350, 340, 420), (400, 390, 470), (450, 435, 520), (500, 485, 570), (600, 580, 670),
        (700, 680, 770), (800, 775, 870), (900, 870, 970), (1000, 970, 1070), (1100, 1070, 1170), (1200, 1165, 1270)
    };

    /// <summary>표준각도 순서는 <see cref="JointDeflectionRule.StandardAngles"/>와 같게 둔다(규격표 행 순서).</summary>
    private static readonly (double AngleDeg, (double Dn, double RadiusMm, double LayingMm)[] Rows)[] AngleTables =
    {
        (11.25, Bend1125), (22.5, Bend225), (45d, Bend45), (90d, Bend90)
    };

    private static IEnumerable<BendFittingEntry> BuildHandbook() =>
        from dn in StraightPipeSpecTable.NominalDiameters
        from form in new[] { BendForm.AType, BendForm.BType }
        from table in AngleTables
        let row = table.Rows.First(x => Math.Abs(x.Dn - dn) <= Epsilon)
        select new BendFittingEntry(
            dn,
            table.AngleDeg,
            form,
            row.LayingMm,
            row.RadiusMm,
            form == BendForm.BType ? SpigotLengthMm : 0d,
            WallThickness.First(x => Math.Abs(x.Dn - dn) <= Epsilon).WallThicknessMm,
            // 타입명은 문서에 로드된 곡관 패밀리에 따라 달라지므로 비워 두고 사용자가 규격표 창에서 고른다.
            TypeName: null);

    public static BendFittingCatalog Default { get; } = new(BuildHandbook().ToList());

    // ── 구 임시값 감지 전용 (2026-08-18~19 사이에 저장된 프로젝트가 대상) ────────────
    // 아래 블록은 배치에 쓰이지 않는다. 저장된 규격이 그 시기의 임시값 그대로인지 판별해
    // [기본값 복원] 안내를 띄우는 데에만 쓴다. 안내가 충분히 돌고 나면 통째로 삭제할 수 있다.

    /// <summary>구 임시표의 형식별 곡률반경 계수(R = 계수 × DN).</summary>
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
            StraightPipeSpecTable.Default.Find(PipeKindCatalog.Water1, dn)?.ThicknessMm ?? 0d,
            TypeName: null);

    private static BendFittingCatalog LegacyPlaceholder { get; } = new(BuildPlaceholders().ToList());
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
