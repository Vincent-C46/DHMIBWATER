using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Gis;
using Xunit;

namespace DHBIMWATER.UI.Tests.Gis;

public class BendResolverTests
{
    private static BendSettings Settings(double toleranceDeg, params BendFittingEntry[] fittings) =>
        new(new BendToleranceTable(new[] { new BendToleranceEntry("", 2000, toleranceDeg) }),
            new BendFittingCatalog(fittings));

    private static NodeClassification Bend(double deflectionDeg, double diameterMm = 100, string pipeKind = "") =>
        new(1, new Point3D(0, 0, 0), NodeKind.Bend, 2, deflectionDeg, diameterMm, diameterMm, pipeKind);

    [Theory]
    [InlineData(45, 5, 45)]      // 정확히 표준각
    [InlineData(43, 5, 45)]      // 45°에 잔차 −2
    [InlineData(16, 5, 11.25)]   // |16−11.25|=4.75 ≤ 5 이므로 11.25° 곡관이다
    public void Deflection_within_tolerance_selects_nearest_standard_angle(double theta, double delta, double expected)
    {
        var result = BendResolver.Resolve(Bend(theta), Settings(delta), BendForm.AType);

        Assert.Equal(BendResolutionKind.Standard, result.Kind);
        Assert.Equal(expected, result.StandardAngleDeg);
        Assert.Equal(theta - expected, result.ResidualDeg, 9);
    }

    [Fact]
    public void Deflection_within_tolerance_of_zero_needs_no_bend()
    {
        var result = BendResolver.Resolve(Bend(3), Settings(5), BendForm.AType);

        Assert.Equal(BendResolutionKind.None, result.Kind);
        Assert.Equal(3, result.ResidualDeg);
    }

    [Theory]
    [InlineData(16, 3, 11.25)]        // 어느 표준각에도 못 미치고 11.25°가 더 가깝다
    [InlineData(17, 5, 22.5)]         // 22.5°가 더 가깝다
    [InlineData(16.875, 5, 11.25)]    // 11.25와 22.5의 정확한 중간 — 동률이면 작은 각
    public void Deflection_outside_every_tolerance_reports_nearest_angle(double theta, double delta, double nearest)
    {
        var result = BendResolver.Resolve(Bend(theta), Settings(delta), BendForm.AType);

        Assert.Equal(BendResolutionKind.Unresolved, result.Kind);
        Assert.Equal(nearest, result.StandardAngleDeg);
        Assert.Equal(0, result.LayingLengthMm);
    }

    [Fact]
    public void Empty_tolerance_table_resolves_nothing_but_exact_angles()
    {
        var empty = new BendSettings(new BendToleranceTable(Array.Empty<BendToleranceEntry>()), BendFittingCatalog.Empty);

        // δ=0이라 표준각에서 조금만 벗어나도 미해결로 남아 설정 누락이 드러난다.
        Assert.Equal(BendResolutionKind.Unresolved, BendResolver.Resolve(Bend(44), empty, BendForm.AType).Kind);
        // 다만 정확히 45.000°면 |θ−a|=0 ≤ 0 이므로 경계 포함이다.
        Assert.Equal(BendResolutionKind.Standard, BendResolver.Resolve(Bend(45), empty, BendForm.AType).Kind);
    }

    [Fact]
    public void ToleranceFor_prefers_matching_pipe_kind_then_diameter_band()
    {
        var table = new BendToleranceTable(new[]
        {
            new BendToleranceEntry("", 450, 5), new BendToleranceEntry("", 800, 4),
            new BendToleranceEntry("DCIP", 450, 2), new BendToleranceEntry("DCIP", 800, 1)
        });

        Assert.Equal(1, table.ToleranceFor("DCIP", 500));   // 관종 일치 + 500 이상 구간
        Assert.Equal(5, table.ToleranceFor("PE", 300));     // 관종 없음 → 전체에서 조회
        Assert.Equal(1, table.ToleranceFor("DCIP", 2000));  // 최대 초과 → 최대 직경 항목
        Assert.Equal(0, new BendToleranceTable(Array.Empty<BendToleranceEntry>()).ToleranceFor("", 100));
    }

    [Fact]
    public void ResolveAll_skips_nodes_that_are_not_bends()
    {
        var nodes = new List<NodeClassification>
        {
            Bend(45),
            new(2, new Point3D(0, 0, 0), NodeKind.Tee, 3, 0, 100, 100, ""),
            new(3, new Point3D(0, 0, 0), NodeKind.EndPoint, 1, 0, 100, 100, "")
        };

        Assert.Single(BendResolver.ResolveAll(nodes, Settings(5), BendForm.AType));
    }

    [Fact]
    public void Find_matches_angle_and_form_before_diameter_band()
    {
        var catalog = new BendFittingCatalog(new[]
        {
            new BendFittingEntry("", 100, 45, BendForm.AType, 130, 210),
            new BendFittingEntry("", 200, 45, BendForm.AType, 180, 300),
            new BendFittingEntry("", 100, 45, BendForm.BType, 330, 210),
            new BendFittingEntry("", 100, 90, BendForm.AType, 200, 210)
        });

        Assert.Equal(130, catalog.Find("", 80, 45, BendForm.AType)!.LayingLengthMm);
        Assert.Equal(330, catalog.Find("", 80, 45, BendForm.BType)!.LayingLengthMm);
        Assert.Equal(200, catalog.Find("", 80, 90, BendForm.AType)!.LayingLengthMm);
        Assert.Null(catalog.Find("", 80, 22.5, BendForm.AType));
    }

    [Fact]
    public void Standard_bend_carries_catalog_size_and_derived_tangent_length()
    {
        // 주철관 핸드북 DN80 45° A형: R=210, t=130 → T = 210·tan22.5° ≈ 86.98
        var result = BendResolver.Resolve(Bend(45, 80), Settings(5, new BendFittingEntry("", 100, 45, BendForm.AType, 130, 210)), BendForm.AType);

        Assert.True(result.HasFittingSize);
        Assert.Equal(130, result.LayingLengthMm);
        Assert.Equal(210, result.CenterlineRadiusMm);
        Assert.Equal(86.98, result.TangentLengthMm, 2);
        Assert.True(result.IsSizeConsistent);
    }

    [Fact]
    public void Standard_bend_without_catalog_entry_has_no_size()
    {
        var result = BendResolver.Resolve(Bend(45), Settings(5), BendForm.AType);

        Assert.Equal(BendResolutionKind.Standard, result.Kind);
        Assert.False(result.HasFittingSize);
        Assert.Equal(0, result.LayingLengthMm);
    }

    [Fact]
    public void Laying_length_shorter_than_tangent_length_is_inconsistent()
    {
        // t=100 < T=210·tan45°=210 → 호가 곡관 몸통 밖으로 나간다. 모델링은 진행하되 경고 대상이다.
        var result = BendResolver.Resolve(Bend(90, 80), Settings(5, new BendFittingEntry("", 100, 90, BendForm.AType, 100, 210)), BendForm.AType);

        Assert.False(result.IsSizeConsistent);
    }

    [Fact]
    public void Arc_midpoint_lies_on_the_centerline_circle()
    {
        // 45° 편각. dirA/dirB는 절점에서 양쪽 직관으로 나가는 단위벡터다.
        var dirA = new Vector3D(-1, 0, 0);
        var dirB = new Vector3D(Math.Cos(Math.PI / 4), Math.Sin(Math.PI / 4), 0);
        var origin = new Point3D(0, 0, 0);

        var arc = BendArcGeometry.Compute(origin, dirA, dirB, 45, 210, 130);

        // P1/P3은 관 끝이므로 절점에서 t = 130mm = 0.130m 떨어져 있다.
        Assert.Equal(0.130, arc.Start.DistanceTo(origin), 12);
        Assert.Equal(0.130, arc.End.DistanceTo(origin), 12);

        // P2는 실제 호 위의 점이다 — 원 중심에서 정확히 R만큼 떨어져야 한다.
        var bisector = new Vector3D(dirA.X + dirB.X, dirA.Y + dirB.Y, dirA.Z + dirB.Z).Normalize();
        var centerDistance = 0.210 / Math.Cos(22.5 * Math.PI / 180);
        var center = new Point3D(bisector.X * centerDistance, bisector.Y * centerDistance, bisector.Z * centerDistance);

        Assert.Equal(0.210, center.DistanceTo(arc.ArcMid), 12);
        Assert.Equal(17.30, arc.ExternalMm, 2);
    }
}
