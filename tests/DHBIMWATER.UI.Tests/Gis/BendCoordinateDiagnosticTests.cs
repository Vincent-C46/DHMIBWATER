using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Gis;
using Xunit;
using Xunit.Abstractions;

namespace DHBIMWATER.UI.Tests.Gis;

/// <summary>진단용 임시 테스트 — 곡관 P2~P3 좌표가 비정상적으로 커지는 문제를 재현한다.</summary>
public class BendCoordinateDiagnosticTests
{
    private readonly ITestOutputHelper _output;
    public BendCoordinateDiagnosticTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void Diagnose_bend_point_distances_for_90_degree_turn()
    {
        var vertices = new[]
        {
            new Point3D(0, 0, 0),
            new Point3D(10, 0, 0),
            new Point3D(10, 10, 0)
        };
        var alignments = new[]
        {
            new PipeAlignment(vertices, PipeKindCatalog.Water1, 200, "test.shp", "1", new Dictionary<string, string>())
        };

        var snapTolerance = 0.01;
        var graph = PipeNetworkBuilder.Build(alignments, snapTolerance);
        var nodes = PipeNetworkClassifier.Classify(graph);
        foreach (var n in nodes)
            _output.WriteLine($"Node {n.NodeId}: Kind={n.Kind} Deflection={n.DeflectionDeg} MaxDn={n.MaxDiameterMm}");

        var settings = BendSettings.Default;
        var resolutions = BendResolver.ResolveAll(nodes, settings);
        foreach (var r in resolutions)
            _output.WriteLine($"Resolution Node={r.NodeId} Kind={r.Kind} StandardAngle={r.StandardAngleDeg} R={r.CenterlineRadiusMm} t={r.LayingLengthMm} T={r.TangentLengthMm} HasSize={r.HasFittingSize}");

        var plan = BendTrimPlanner.Plan(alignments, graph, resolutions, snapTolerance);
        Assert.NotEmpty(plan.Placements);

        foreach (var p in plan.Placements)
        {
            var pts = p.Points;
            _output.WriteLine($"Node {p.NodeId}: P1={pts.Start} P2={pts.ArcStart} P3={pts.ArcMid} P4={pts.ArcEnd} P5={pts.End}");
            _output.WriteLine($"  E={pts.ExternalMm}mm T={pts.TangentMm}mm");
            _output.WriteLine($"  |P2-P3| = {pts.ArcStart.DistanceTo(pts.ArcMid)} m");
            _output.WriteLine($"  |P1-P5| = {pts.Start.DistanceTo(pts.End)} m");

            // 물리적으로 DN200 곡관 하나가 수십m를 넘을 수 없다.
            Assert.True(pts.ArcStart.DistanceTo(pts.ArcMid) < 5, $"P2-P3 distance too large: {pts.ArcStart.DistanceTo(pts.ArcMid)}");
        }
    }
}
