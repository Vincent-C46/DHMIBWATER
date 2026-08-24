using System.Text.Json.Serialization;

namespace DHBIMWATER.Core.Gis;

/// <summary>적용 규격이 정의하는 이형관 형식 구분. Joint Type과는 별개다.</summary>
public enum BendForm { AType, BType }

/// <summary>이형관 접합 방식. 핸드북에서 소켓곡관과 플랜지곡관은 R·t가 다른 별도 표다(e·s는 같다).</summary>
public enum BendConnection { Socket, Flanged }

/// <param name="DiameterMm">호칭지름 DN. 조회는 Exact Match다.</param>
/// <param name="Form">
/// A형(양쪽 소켓)/B형(소켓+스피것). 핸드북상 e·R·t는 형식과 무관하게 같고 s(스피것 길이)·무게만 다르다
/// (2026-08-19 확인). 행 하나가 한 형식만 대표하므로 기본은 B형이고, A형이 필요한 자리만 사용자가 바꾼다.
/// </param>
/// <param name="WallThicknessMm">e — 곡관 자체의 규격상 벽두께(mm).</param>
/// <param name="WeightKpMechanicalKg">KP 메커니컬 조인트 기준 무게(kg). 현재 <see cref="Form"/> 값에 대응한다.</param>
/// <param name="WeightTytonKg">타이튼 조인트 기준 무게(kg). 현재 <see cref="Form"/> 값에 대응한다.</param>
/// <param name="Material">
/// 이 규격이 속한 관종. DN·각도만으로는 관종이 구분되지 않아 조회 키에 필요하다.
/// 기본값이 있으므로 관종 축 도입 이전에 저장된 JSON도 그대로 역직렬화된다.
/// </param>
/// <param name="Connection">소켓/플랜지. 기본값이 있어 이 축 도입 이전 JSON도 소켓으로 읽힌다.</param>
public sealed record BendFittingEntry(
    double DiameterMm,
    double AngleDeg,
    BendForm Form,
    double LayingLengthMm,
    double CenterlineRadiusMm,
    double WallThicknessMm = 0d,
    double WeightKpMechanicalKg = 0d,
    double WeightTytonKg = 0d,
    PipeMaterial Material = PipeMaterial.DuctileIron,
    BendConnection Connection = BendConnection.Socket)
{
    /// <summary>s — B형(소켓+스피것)만 200mm 고정, A형(양쪽 소켓)은 0. 핸드북 고정값이라 사용자 입력 대상이 아니다.</summary>
    [JsonIgnore]
    public double ExtraLegLengthMm => Form == BendForm.BType ? 200d : 0d;
}

public sealed class BendFittingCatalog
{
    private const double Epsilon = 1e-6;

    public BendFittingCatalog(IReadOnlyList<BendFittingEntry> entries) => Entries = entries;
    public IReadOnlyList<BendFittingEntry> Entries { get; }

    /// <summary>
    /// 저장된 곡관표가 현행 규격 구조와 맞지 않아 [기본값 복원] 안내가 필요한 상태인지.
    /// ① 08-18자 임시표(R = 계수 × DN) ② 08-19자 A형·B형 이중행 ③ 접합종류 축 이전(플랜지 행 없음).
    /// </summary>
    public bool NeedsRestore => Entries.SequenceEqual(LegacyPlaceholder.Entries)
        || Entries.GroupBy(x => (x.Material, x.Connection, x.DiameterMm, x.AngleDeg)).Any(x => x.Count() > 1)
        || !Entries.Any(x => x.Connection == BendConnection.Flanged);

    /// <summary>DN·각도·접합종류로 조회한다. 형식(A/B)은 행 자체가 갖고 있으므로 조회 키가 아니다.</summary>
    public BendFittingEntry? Find(double diameterMm, double angleDeg, PipeMaterial material = PipeMaterial.DuctileIron,
        BendConnection connection = BendConnection.Socket) => Entries.FirstOrDefault(x =>
        x.Material == material
        && x.Connection == connection
        && Math.Abs(x.DiameterMm - diameterMm) <= Epsilon
        && Math.Abs(x.AngleDeg - angleDeg) <= Epsilon);

    // ══════════════════════════════════════════════════════════════════════════
    // 주철관 핸드북(2020년판) 소켓곡관 규격 — 출처: docs/주철관핸드북(2020년판)_이형관.pdf
    //   Ⅴ장 4. 90° / 5. 45° / 6. 22½° / 7. 11¼° 소켓곡관 표 (DN80~1200)
    //
    // 열 대응
    //   e → WallThicknessMm, R → CenterlineRadiusMm, t → LayingLengthMm(절점→관 끝), s → ExtraLegLengthMm(계산값)
    //
    // 판단 근거 (2026-08-19 / 2026-08-20 사용자 확인)
    //   · A형(양쪽 소켓)과 B형(소켓+스피것)은 치수 e·R·t가 동일하고 s·무게만 다르다 → 행은 DN×각도 72개로 두고
    //     형식별 무게만 따로 둔다. s=200은 도면상 B형 스피것 쪽에만 표기된다 → A형 0, B형 200(계산값).
    //   · 표에 DE(외경) 열이 없다 → 곡관 외경은 직관 제원표(StraightPipeSpecTable)의 DE를 쓴다. DE는 관종 무관 DN 단일값이다.
    //   · 이형관 벽두께 e는 관종과 무관한 단일값이며 4개 각도가 모두 같다. 직관 두께(관종별)와는 다른 계열이다.
    //   · 무게는 조인트 종류(KP메커니컬/타이튼)·형식(A/B)별로 다르다. 핸드북에는 KP-L(장소켓) 무게도 있으나
    //     시스템이 다루는 조인트가 KP메커니컬·타이튼 2종뿐이라(JointTypeCatalog) 이번에는 반영하지 않는다.
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>이형관 벽두께 e — DN만의 함수다(각도·관종 무관).</summary>
    private static readonly (double Dn, double WallThicknessMm)[] WallThickness =
    {
        (80, 7.0), (100, 7.2), (125, 7.5), (150, 7.8), (200, 8.4), (250, 9.0),
        (300, 9.6), (350, 10.2), (400, 10.8), (450, 11.4), (500, 12.0), (600, 13.2),
        (700, 14.4), (800, 15.6), (900, 16.8), (1000, 18.0), (1100, 19.2), (1200, 20.4)
    };

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

    /// <summary>90° 플랜지곡관 — (DN, R, t). 출처 핸드북 p.105 (13. 90° FLANGED BEND).</summary>
    private static readonly (double Dn, double RadiusMm, double LayingMm)[] Flanged90 =
    {
        (80, 122, 165), (100, 135, 180), (125, 152.5, 200), (150, 170, 220), (200, 205, 260), (250, 290, 350),
        (300, 335, 400), (350, 380, 450), (400, 425, 500), (450, 470, 550), (500, 515, 600), (600, 605, 700),
        (700, 695, 800), (800, 785, 900), (900, 875, 1000), (1000, 965, 1100), (1100, 1055, 1200), (1200, 1145, 1300)
    };

    /// <summary>45° 플랜지곡관 — (DN, R, t). 출처 핸드북 p.106 (14. 45° FLANGED BEND).</summary>
    // TODO: 핸드북 원문 확인 필요 — DN250·300·350 R·t 비단조(인쇄 오류 의심). 원문 그대로 전사했다.
    private static readonly (double Dn, double RadiusMm, double LayingMm)[] Flanged45 =
    {
        (80, 210, 130), (100, 230, 140), (125, 250, 150), (150, 265, 160), (200, 300, 180), (250, 700, 350),
        (300, 809, 400), (350, 550, 300), (400, 600, 325), (450, 650, 350), (500, 700, 375), (600, 800, 425),
        (700, 900, 480), (800, 1000, 530), (900, 1100, 580), (1000, 1200, 630), (1100, 1300, 695), (1200, 1400, 750)
    };

    private static readonly (double AngleDeg, (double Dn, double RadiusMm, double LayingMm)[] Rows)[] FlangedAngleTables =
    {
        (45d, Flanged45), (90d, Flanged90)
    };

    /// <summary>접합종류별 수록 각도. 플랜지곡관은 핸드북에 90°·45°만 있다(11¼·22½ 플랜지 표는 존재하지 않음).</summary>
    public static IReadOnlyList<double> AnglesFor(BendConnection connection) =>
        connection == BendConnection.Flanged ? new[] { 45d, 90d } : JointDeflectionRule.StandardAngles;

    /// <summary>11¼° 소켓곡관 무게 — (DN, KP메커니컬 A형, KP메커니컬 B형, 타이튼 A형, 타이튼 B형) kg.</summary>
    private static readonly (double Dn, double KpA, double KpB, double TytonA, double TytonB)[] Weight1125 =
    {
        (80, 9.6, 8.4, 8.3, 7.4), (100, 11.6, 10.3, 10.5, 9.3), (125, 14.5, 13.1, 13.8, 12.2), (150, 18.2, 16.4, 17.6, 15.4),
        (200, 25.0, 23.0, 25.0, 22.0), (250, 34.5, 32.0, 35.5, 31.0), (300, 44.0, 41.0, 47.0, 40.5), (350, 57.5, 52.5, 60.5, 52.0),
        (400, 70.5, 64.0, 71.5, 62.0), (450, 86.0, 78.0, 93.5, 78.5), (500, 104, 93.5, 108, 92.0), (600, 149, 131, 143, 123),
        (700, 202, 175, 184, 160), (800, 248, 219, 234, 205), (900, 303, 271, 298, 259), (1000, 372, 332, 382, 326),
        (1100, 436, 393, 460, 393), (1200, 532, 475, 562, 476)
    };

    /// <summary>22½° 소켓곡관 무게 — (DN, KP메커니컬 A형, KP메커니컬 B형, 타이튼 A형, 타이튼 B형) kg.</summary>
    private static readonly (double Dn, double KpA, double KpB, double TytonA, double TytonB)[] Weight225 =
    {
        (80, 9.8, 8.6, 8.5, 7.6), (100, 11.9, 10.6, 10.8, 9.6), (125, 15.2, 13.8, 14.5, 12.9), (150, 19.3, 17.5, 18.7, 16.5),
        (200, 27.0, 25.0, 27.0, 24.0), (250, 37.5, 34.5, 38.0, 33.5), (300, 48.0, 45.0, 51.0, 44.5), (350, 63.0, 58.0, 66.0, 57.5),
        (400, 79.0, 73.0, 80.0, 71.0), (450, 97.5, 89.5, 105, 90.0), (500, 119, 108, 123, 107), (600, 172, 154, 166, 146),
        (700, 239, 211, 220, 196), (800, 295, 267, 282, 253), (900, 371, 338, 366, 327), (1000, 461, 420, 471, 415),
        (1100, 558, 515, 582, 515), (1200, 679, 623, 709, 624)
    };

    /// <summary>45° 소켓곡관 무게 — (DN, KP메커니컬 A형, KP메커니컬 B형, 타이튼 A형, 타이튼 B형) kg.</summary>
    private static readonly (double Dn, double KpA, double KpB, double TytonA, double TytonB)[] Weight45 =
    {
        (80, 10.2, 9.0, 8.9, 8.0), (100, 12.7, 11.4, 11.6, 10.4), (125, 16.2, 14.8, 15.5, 13.9), (150, 21.0, 19.0, 20.0, 18.0),
        (200, 30.0, 28.0, 30.5, 27.5), (250, 42.5, 40.0, 43.5, 39.0), (300, 56.0, 53.0, 59.0, 52.5), (350, 75.0, 70.0, 78.0, 69.5),
        (400, 94.5, 88.5, 95.5, 86.5), (450, 119, 111, 127, 112), (500, 147, 136, 151, 135), (600, 217, 199, 211, 191),
        (700, 304, 277, 285, 262), (800, 387, 358, 373, 344), (900, 494, 461, 489, 450), (1000, 626, 585, 636, 580),
        (1100, 758, 715, 782, 715), (1200, 950, 893, 980, 894)
    };

    /// <summary>90° 소켓곡관 무게 — (DN, KP메커니컬 A형, KP메커니컬 B형, 타이튼 A형, 타이튼 B형) kg.</summary>
    private static readonly (double Dn, double KpA, double KpB, double TytonA, double TytonB)[] Weight90 =
    {
        (80, 11.8, 10.6, 9.8, 8.9), (100, 14.9, 13.6, 12.9, 11.7), (125, 19.5, 18.1, 17.6, 16.0), (150, 25.5, 23.5, 23.5, 21.0),
        (200, 38.0, 36.0, 36.0, 33.0), (250, 55.0, 52.5, 53.5, 49.0), (300, 75.0, 71.5, 74.5, 68.0), (350, 101, 96.0, 100, 91.0),
        (400, 130, 123, 126, 116), (450, 165, 156, 166, 151), (500, 206, 195, 202, 186), (600, 307, 289, 292, 272),
        (700, 435, 408, 405, 381), (800, 573, 544, 545, 516), (900, 744, 712, 722, 683), (1000, 953, 912, 942, 887),
        (1100, 1178, 1135, 1178, 1111), (1200, 1475, 1418, 1477, 1391)
    };

    private static readonly (double AngleDeg, (double Dn, double KpA, double KpB, double TytonA, double TytonB)[] Rows)[] WeightTables =
    {
        (11.25, Weight1125), (22.5, Weight225), (45d, Weight45), (90d, Weight90)
    };

    /// <summary>핸드북 소켓곡관 무게표에서 (DN, 각도, 형식)에 해당하는 조인트별 무게를 찾는다. 규격표 창에서 행의 형식을 바꿀 때도 이 값으로 다시 채운다.</summary>
    public static bool TryGetHandbookWeight(double diameterMm, double angleDeg, BendForm form, out double kpMechanicalKg, out double tytonKg)
    {
        var table = WeightTables.FirstOrDefault(x => Math.Abs(x.AngleDeg - angleDeg) <= Epsilon).Rows;
        var row = table?.FirstOrDefault(x => Math.Abs(x.Dn - diameterMm) <= Epsilon);
        if (table is null || row is null || row.Value == default)
        {
            kpMechanicalKg = 0d; tytonKg = 0d; return false;
        }
        kpMechanicalKg = form == BendForm.AType ? row.Value.KpA : row.Value.KpB;
        tytonKg = form == BendForm.AType ? row.Value.TytonA : row.Value.TytonB;
        return true;
    }

    private static IEnumerable<BendFittingEntry> BuildHandbook()
    {
        foreach (var dn in StraightPipeSpecTable.NominalDiameters)
            foreach (var table in AngleTables)
            {
                var row = table.Rows.First(x => Math.Abs(x.Dn - dn) <= Epsilon);
                TryGetHandbookWeight(dn, table.AngleDeg, BendForm.BType, out var kpKg, out var tytonKg);
                yield return new BendFittingEntry(
                    dn,
                    table.AngleDeg,
                    BendForm.BType,
                    row.LayingMm,
                    row.RadiusMm,
                    WallThickness.First(x => Math.Abs(x.Dn - dn) <= Epsilon).WallThicknessMm,
                    kpKg,
                    tytonKg,
                    Connection: BendConnection.Socket);
            }

        foreach (var dn in StraightPipeSpecTable.NominalDiameters)
            foreach (var table in FlangedAngleTables)
            {
                var row = table.Rows.First(x => Math.Abs(x.Dn - dn) <= Epsilon);
                yield return new BendFittingEntry(
                    dn,
                    table.AngleDeg,
                    BendForm.BType,
                    row.LayingMm,
                    row.RadiusMm,
                    WallThickness.First(x => Math.Abs(x.Dn - dn) <= Epsilon).WallThicknessMm,
                    Connection: BendConnection.Flanged);
            }
    }

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
            StraightPipeSpecTable.Default.Find(PipeKindCatalog.Water1, dn)?.ThicknessMm ?? 0d);

    private static BendFittingCatalog LegacyPlaceholder { get; } = new(BuildPlaceholders().ToList());
}

/// <summary>관로 규격과 판정 설정의 저장 단위.</summary>
public sealed record BendSettings(
    StraightPipeSpecTable StraightPipes,
    JointDeflectionTable JointDeflections,
    BendFittingCatalog Fittings,
    string ActiveJointType,
    JointApplicationMode ApplicationMode,
    /// <summary>배치에 쓸 곡관 접합 종류. 모델링 창에서 고르며 기본은 소켓이다.</summary>
    BendConnection ActiveBendConnection = BendConnection.Socket)
{
    public static BendSettings Default { get; } = new(
        StraightPipeSpecTable.Default,
        JointDeflectionTable.Default,
        BendFittingCatalog.Default,
        JointTypeCatalog.KpMechanical,
        JointApplicationMode.SingleJoint);
}
