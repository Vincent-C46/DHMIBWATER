using System;
using System.Linq;
using DHBIMWATER.Core.Gis;
using Xunit;

namespace DHBIMWATER.UI.Tests.Gis;

public class BendDefaultTablesTests
{
    /// <summary>사용자 제공 허용굴곡표(2026-08-18)의 구간 경계 DN을 직접 확인한다.</summary>
    [Theory]
    // KP 메커니컬 — 80~150 / 200~300 / 350~500 / 600~700 / 800~1200
    [InlineData("KP 메커니컬 조인트", 80, 5.0)]
    [InlineData("KP 메커니컬 조인트", 150, 5.0)]
    [InlineData("KP 메커니컬 조인트", 200, 4.0)]
    [InlineData("KP 메커니컬 조인트", 300, 4.0)]
    [InlineData("KP 메커니컬 조인트", 350, 3.0)]
    [InlineData("KP 메커니컬 조인트", 500, 3.0)]
    [InlineData("KP 메커니컬 조인트", 600, 2.0)]
    [InlineData("KP 메커니컬 조인트", 700, 2.0)]
    [InlineData("KP 메커니컬 조인트", 800, 1.5)]
    [InlineData("KP 메커니컬 조인트", 1200, 1.5)]
    // 타이튼 — 80~300 / 350~400 / 450~600 / 700~900 / 1000~1200
    [InlineData("타이튼 조인트", 80, 5.0)]
    [InlineData("타이튼 조인트", 300, 5.0)]
    [InlineData("타이튼 조인트", 350, 4.0)]
    [InlineData("타이튼 조인트", 400, 4.0)]
    [InlineData("타이튼 조인트", 450, 3.0)]
    [InlineData("타이튼 조인트", 600, 3.0)]
    [InlineData("타이튼 조인트", 700, 2.5)]
    [InlineData("타이튼 조인트", 900, 2.5)]
    [InlineData("타이튼 조인트", 1000, 2.0)]
    [InlineData("타이튼 조인트", 1200, 2.0)]
    public void JointDeflection_default_matches_handbook_ranges(string jointType, double dn, double expected)
        => Assert.Equal(expected, JointDeflectionTable.Default.AllowableFor(jointType, dn));

    [Fact]
    public void JointDeflection_default_covers_every_dn_for_both_joint_types()
    {
        foreach (var jointType in JointTypeCatalog.All)
            foreach (var dn in StraightPipeSpecTable.NominalDiameters)
                Assert.NotNull(JointDeflectionTable.Default.AllowableFor(jointType, dn));
    }

    /// <summary>배관길이 t가 접선길이보다 짧으면 호가 곡관 몸통 밖으로 나가 전 절점이 치수 불일치로 보고된다.</summary>
    [Fact]
    public void Handbook_fittings_are_size_consistent_for_every_dn_angle_form()
    {
        foreach (var entry in BendFittingCatalog.Default.Entries)
        {
            var tangent = BendResolver.TangentLength(entry.CenterlineRadiusMm, entry.AngleDeg);
            Assert.True(entry.LayingLengthMm >= tangent,
                $"DN{entry.DiameterMm} {entry.AngleDeg}° {entry.Form}: 배관길이 {entry.LayingLengthMm} < 접선길이 {tangent}");
            Assert.Equal(PipeMaterial.DuctileIron, entry.Material);
        }
    }

    /// <summary>핸드북 소켓곡관 표(docs/주철관핸드북(2020년판)_이형관.pdf)의 대표 행을 직접 확인한다.</summary>
    [Theory]
    // 4. 90° 소켓곡관
    [InlineData(80, 90, 7.0, 75, 150)]
    [InlineData(1200, 90, 20.4, 1165, 1270)]
    // 5. 45° 소켓곡관
    [InlineData(300, 45, 9.6, 300, 175)]
    [InlineData(1200, 45, 20.4, 1195, 575)]
    // 6. 22½° 소켓곡관
    [InlineData(500, 22.5, 12.0, 495, 155)]
    // 7. 11¼° 소켓곡관
    [InlineData(100, 11.25, 7.2, 115, 55)]
    [InlineData(1200, 11.25, 20.4, 1195, 175)]
    public void Bend_default_matches_handbook_rows(double dn, double angle, double e, double radius, double laying)
    {
        var entry = BendFittingCatalog.Default.Find(dn, angle);
        Assert.NotNull(entry);
        Assert.Equal(e, entry!.WallThicknessMm);
        Assert.Equal(radius, entry.CenterlineRadiusMm);
        Assert.Equal(laying, entry.LayingLengthMm);
    }

    /// <summary>
    /// 행은 DN×각도 하나뿐이다(2026-08-20, A형/B형 중복 행 폐지). 기본 형식은 B형이고,
    /// A형으로 바꿔도 e·R·t는 그대로이며 s(스피것)·무게만 형식을 따라간다.
    /// </summary>
    [Fact]
    public void Bend_default_has_one_row_per_dn_and_angle_defaulting_to_b_form()
    {
        foreach (var dn in StraightPipeSpecTable.NominalDiameters)
            foreach (var angle in JointDeflectionRule.StandardAngles)
            {
                var entry = BendFittingCatalog.Default.Find(dn, angle);
                Assert.NotNull(entry);
                Assert.Equal(BendForm.BType, entry!.Form);
                Assert.Equal(200d, entry.ExtraLegLengthMm);

                Assert.True(BendFittingCatalog.TryGetHandbookWeight(dn, angle, BendForm.AType, out var kpA, out var tytonA));
                Assert.True(BendFittingCatalog.TryGetHandbookWeight(dn, angle, BendForm.BType, out var kpB, out var tytonB));
                Assert.Equal(kpB, entry.WeightKpMechanicalKg);
                Assert.Equal(tytonB, entry.WeightTytonKg);
                // A형은 스피것이 없어 더 가볍다(같은 DN·각도).
                Assert.True(kpA > kpB);
                Assert.True(tytonA > tytonB);
            }
        Assert.Equal(StraightPipeSpecTable.NominalDiameters.Count * JointDeflectionRule.StandardAngles.Count,
            BendFittingCatalog.Default.Entries.Count);
    }

    /// <summary>이형관 벽두께 e는 각도와 무관한 DN 단일값이며, 관종별 직관 두께와는 다른 계열이다.</summary>
    [Fact]
    public void Bend_wall_thickness_depends_on_dn_only()
    {
        foreach (var dn in StraightPipeSpecTable.NominalDiameters)
        {
            var thicknesses = JointDeflectionRule.StandardAngles
                .Select(angle => BendFittingCatalog.Default.Find(dn, angle)!.WallThicknessMm)
                .Distinct()
                .ToList();
            Assert.Single(thicknesses);
        }
        // DN300: 이형관 9.6 vs 상수 1종관 직관 8.8
        Assert.Equal(9.6, BendFittingCatalog.Default.Find(300, 45)!.WallThicknessMm);
        Assert.Equal(8.8, StraightPipeSpecTable.Default.Find(PipeKindCatalog.Water1, 300)!.ThicknessMm);
    }

    /// <summary>핸드북 반영 이전의 임시값이 저장된 프로젝트만 [기본값 복원] 안내 대상이다.</summary>
    [Fact]
    public void Legacy_placeholder_is_detected_only_for_the_old_temporary_table()
    {
        Assert.False(BendFittingCatalog.Default.IsLegacyPlaceholder);

        var legacy = new BendFittingCatalog((
            from dn in StraightPipeSpecTable.NominalDiameters
            from form in new[] { (Form: BendForm.AType, Factor: 2.5), (Form: BendForm.BType, Factor: 1.5) }
            from angle in JointDeflectionRule.StandardAngles
            let radius = Math.Round(form.Factor * dn, 1)
            select new BendFittingEntry(dn, angle, form.Form,
                Math.Ceiling(BendResolver.TangentLength(radius, angle)) + 50d, radius,
                StraightPipeSpecTable.Default.Find(PipeKindCatalog.Water1, dn)?.ThicknessMm ?? 0d)).ToList());
        Assert.True(legacy.IsLegacyPlaceholder);
    }

    [Fact]
    public void Straight_outer_diameter_can_be_resolved_by_dn_only()
    {
        var found = StraightPipeSpecTable.Default.FindOuterDiameterByDiameter(300);
        Assert.NotNull(found);
        Assert.Equal(326d, found!.Value.OuterDiameterMm);
        Assert.Equal(PipeKindCatalog.Water1, found.Value.PipeKind);
        Assert.Null(StraightPipeSpecTable.Default.FindOuterDiameterByDiameter(75));
    }
}
