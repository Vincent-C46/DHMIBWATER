using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Gis;
using Xunit;

namespace DHBIMWATER.UI.Tests.Gis;

public class BendResolverTests
{
    private static BendSettings Settings(double allowableDeg, JointApplicationMode mode = JointApplicationMode.SingleJoint,
        string activeJointType = JointTypeCatalog.KpMechanical, BendConnection connection = BendConnection.Socket,
        params BendFittingEntry[] fittings) => new(
        StraightPipeSpecTable.Default,
        new JointDeflectionTable(new[] { new JointDeflectionSpec(activeJointType, 100, allowableDeg) }),
        new BendFittingCatalog(fittings), activeJointType, mode, connection);

    private static NodeClassification Bend(double deflectionDeg, double diameterMm = 100, string pipeKind = PipeKindCatalog.Water1) =>
        new(1, new Point3D(0, 0, 0), NodeKind.Bend, 2, deflectionDeg, diameterMm, diameterMm, pipeKind);

    [Theory]
    [InlineData(45, 5, 45)]
    [InlineData(43, 5, 45)]
    [InlineData(16, 5, 11.25)]
    public void Deflection_within_allowable_selects_standard_angle(double theta, double allowable, double expected)
    {
        var result = BendResolver.Resolve(Bend(theta), Settings(allowable));
        Assert.Equal(BendResolutionKind.Standard, result.Kind);
        Assert.Equal(expected, result.StandardAngleDeg);
        Assert.True(result.IsAcceptable);
    }

    [Fact]
    public void Deflection_inside_joint_allowable_needs_no_bend()
    {
        var result = BendResolver.Resolve(Bend(3), Settings(5));
        Assert.Equal(BendResolutionKind.None, result.Kind);
        Assert.True(result.IsAcceptable);
    }

    [Fact]
    public void Flanged_mode_limits_candidates_and_uses_flanged_dimensions()
    {
        var flanged = new BendFittingEntry(100, 45, BendForm.BType, 140, 230, Connection: BendConnection.Flanged);
        var result = BendResolver.Resolve(Bend(22.5), Settings(3, connection: BendConnection.Flanged, fittings: flanged));

        Assert.Equal(BendResolutionKind.Unresolved, result.Kind);
        Assert.Equal(45, result.StandardAngleDeg);
        Assert.Equal(140, result.LayingLengthMm);
        Assert.Equal(230, result.CenterlineRadiusMm);
    }

    [Fact]
    public void Unacceptable_bend_keeps_nearest_fitting_size_for_placement()
    {
        var fitting = new BendFittingEntry(100, 11.25, BendForm.AType, 130, 210);
        var result = BendResolver.Resolve(Bend(16), Settings(3, fittings: fitting));
        Assert.Equal(BendResolutionKind.Unresolved, result.Kind);
        Assert.False(result.IsAcceptable);
        Assert.True(result.HasFittingSize);
        Assert.Equal(130, result.LayingLengthMm);
        Assert.Equal(210, result.CenterlineRadiusMm);
    }

    [Fact]
    public void Missing_joint_setting_marks_even_exact_angle_unacceptable()
    {
        // 허용굴곡 미설정 상태를 빈 표로 명시한다.
        // (2026-08-18 이전에는 JointDeflectionTable.Default가 빈 표라 그것을 썼지만, 이제 핸드북 값이 들어 있다.)
        var settings = new BendSettings(StraightPipeSpecTable.Default, new JointDeflectionTable(Array.Empty<JointDeflectionSpec>()),
            new BendFittingCatalog(new[] { new BendFittingEntry(100, 45, BendForm.AType, 130, 210) }),
            JointTypeCatalog.KpMechanical, JointApplicationMode.SingleJoint);
        var result = BendResolver.Resolve(Bend(45), settings);
        Assert.Equal(BendResolutionKind.Unresolved, result.Kind);
        Assert.False(result.IsAcceptable);
        Assert.Equal(0, result.EffectiveAllowableDeg);
    }

    [Fact]
    public void Both_joints_add_two_individually_looked_up_allowances()
    {
        var table = new JointDeflectionTable(new[] { new JointDeflectionSpec(JointTypeCatalog.KpMechanical, 100, 3) });
        Assert.Equal(3, table.EffectiveAllowableFor(JointTypeCatalog.KpMechanical, 100, JointApplicationMode.SingleJoint));
        Assert.Equal(6, table.EffectiveAllowableFor(JointTypeCatalog.KpMechanical, 100, JointApplicationMode.BothJoints));
    }

    [Fact]
    public void All_three_spec_tables_use_exact_dn_match()
    {
        var straight = new StraightPipeSpecTable(new[]
        {
            new StraightPipeSpec(PipeKindCatalog.Water1, 300, 322.8, 10),
            new StraightPipeSpec(PipeKindCatalog.Water1, 800, 842, 14)
        });
        var fittings = new BendFittingCatalog(new[]
        {
            new BendFittingEntry(300, 45, BendForm.AType, 100, 200),
            new BendFittingEntry(800, 45, BendForm.AType, 200, 400)
        });
        var joints = new JointDeflectionTable(new[]
        {
            new JointDeflectionSpec(JointTypeCatalog.KpMechanical, 300, 4),
            new JointDeflectionSpec(JointTypeCatalog.KpMechanical, 800, 3)
        });
        Assert.Null(straight.Find(PipeKindCatalog.Water1, 600));
        Assert.Null(fittings.Find(600, 45));
        Assert.Null(joints.AllowableFor(JointTypeCatalog.KpMechanical, 600));
    }

    [Fact]
    public void Pipe_kind_does_not_affect_fitting_or_joint_lookup()
    {
        var settings = Settings(5, fittings: new BendFittingEntry(100, 45, BendForm.AType, 130, 210));
        var water = BendResolver.Resolve(Bend(45, pipeKind: PipeKindCatalog.Water1), settings);
        var sewer = BendResolver.Resolve(Bend(45, pipeKind: PipeKindCatalog.Sewer3), settings);
        Assert.Equal(water.LayingLengthMm, sewer.LayingLengthMm);
        Assert.Equal(water.EffectiveAllowableDeg, sewer.EffectiveAllowableDeg);
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
        Assert.Single(BendResolver.ResolveAll(nodes, Settings(5)));
    }

    [Fact]
    public void Laying_length_shorter_than_tangent_length_is_inconsistent()
    {
        var result = BendResolver.Resolve(Bend(90), Settings(5, fittings: new BendFittingEntry(100, 90, BendForm.AType, 100, 210)));
        Assert.False(result.IsSizeConsistent);
    }

    [Fact]
    public void Arc_midpoint_lies_on_the_centerline_circle()
    {
        var dirA = new Vector3D(-1, 0, 0);
        var dirB = new Vector3D(Math.Cos(Math.PI / 4), Math.Sin(Math.PI / 4), 0);
        var origin = new Point3D(0, 0, 0);
        var arc = BendArcGeometry.Compute(origin, dirA, dirB, 45, 210, 130);
        Assert.Equal(0.130, arc.Start.DistanceTo(origin), 12);
        Assert.Equal(0.130, arc.End.DistanceTo(origin), 12);
        var bisector = new Vector3D(dirA.X + dirB.X, dirA.Y + dirB.Y, dirA.Z + dirB.Z).Normalize();
        var centerDistance = 0.210 / Math.Cos(22.5 * Math.PI / 180);
        var center = new Point3D(bisector.X * centerDistance, bisector.Y * centerDistance, bisector.Z * centerDistance);
        Assert.Equal(0.210, center.DistanceTo(arc.ArcMid), 12);
        Assert.Equal(17.30, arc.ExternalMm, 2);
    }

    [Fact]
    public void Bend_orientation_follows_each_horizontal_control_point()
    {
        var tangents = BendOrientation.Tangents(
            new Vector3D(-1, 0, 0),
            new Vector3D(0, 1, 0),
            90);
        var rotations = tangents.Select(BendOrientation.Compute).ToList();

        var expected = new[] { 0d, 0d, 45d, 90d, 90d };
        for (var i = 0; i < expected.Length; i++) Assert.Equal(expected[i], rotations[i].RotXYDeg, 8);
        Assert.All(rotations, x => Assert.Equal(0d, x.RotXZDeg, 8));
    }

    [Fact]
    public void Bend_orientation_preserves_vertical_slope_at_straight_legs()
    {
        var rise = Math.Sqrt(0.5);
        var tangents = BendOrientation.Tangents(
            new Vector3D(-rise, 0, -rise),
            new Vector3D(0, rise, rise),
            90);
        var rotations = tangents.Select(BendOrientation.Compute).ToList();

        Assert.Equal(45d, rotations[0].RotXZDeg, 8);
        Assert.Equal(45d, rotations[1].RotXZDeg, 8);
        Assert.Equal(45d, rotations[3].RotXZDeg, 8);
        Assert.Equal(45d, rotations[4].RotXZDeg, 8);
    }
}
