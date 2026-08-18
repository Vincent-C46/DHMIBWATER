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

    /// <summary>임시 곡관값이라도 접선길이보다 배관길이가 짧으면 전 절점이 치수 불일치로 보고된다.</summary>
    [Fact]
    public void Placeholder_fittings_are_size_consistent_for_every_dn_angle_form()
    {
        Assert.True(BendFittingCatalog.Default.IsPlaceholder);
        foreach (var entry in BendFittingCatalog.Default.Entries)
        {
            var tangent = BendResolver.TangentLength(entry.CenterlineRadiusMm, entry.AngleDeg);
            Assert.True(entry.LayingLengthMm >= tangent,
                $"DN{entry.DiameterMm} {entry.AngleDeg}° {entry.Form}: 배관길이 {entry.LayingLengthMm} < 접선길이 {tangent}");
            Assert.Equal(PipeMaterial.DuctileIron, entry.Material);
        }
    }

    [Fact]
    public void Placeholder_flag_turns_off_when_catalog_is_edited()
    {
        var edited = new BendFittingCatalog(
            BendFittingCatalog.Default.Entries.Skip(1).ToList());
        Assert.False(edited.IsPlaceholder);
    }
}
