namespace DHBIMWATER.Core.Gis;

public enum JointApplicationMode { SingleJoint, BothJoints }

public static class JointTypeCatalog
{
    public const string KpMechanical = "KP 메커니컬 조인트";
    public const string Tyton = "타이튼 조인트";
    public static IReadOnlyList<string> All { get; } = new[] { KpMechanical, Tyton };
    // TODO: 실제 프로젝트에서 사용하는 Joint Type 목록 확장 여부 확인.
}

public sealed record JointDeflectionSpec(string JointType, double DiameterMm, double AllowableDeg);

public sealed class JointDeflectionTable
{
    private const double Epsilon = 1e-6;

    public JointDeflectionTable(IReadOnlyList<JointDeflectionSpec> entries) => Entries = entries;
    public IReadOnlyList<JointDeflectionSpec> Entries { get; }

    public double? AllowableFor(string jointType, double diameterMm) => Entries.FirstOrDefault(x =>
        string.Equals(x.JointType, jointType, StringComparison.OrdinalIgnoreCase)
        && Math.Abs(x.DiameterMm - diameterMm) <= Epsilon)?.AllowableDeg;

    public double? EffectiveAllowableFor(string jointType, double diameterMm, JointApplicationMode mode)
    {
        if (AllowableFor(jointType, diameterMm) is not { } thetaStart) return null;
        if (mode == JointApplicationMode.SingleJoint) return thetaStart;
        if (AllowableFor(jointType, diameterMm) is not { } thetaEnd) return null;
        return thetaStart + thetaEnd;
    }

    // ── 조인트 허용굴곡각 (사용자 제공 2026-08-18) ──────────────────────────────
    // 원표는 DN 구간별 값이라, 구간을 StraightPipeSpecTable.NominalDiameters의 DN 하나하나로 펼쳐 담는다.
    // 조회가 Exact Match라서 표에 없는 DN은 허용굴곡 미설정으로 취급된다.

    /// <summary>(하한 DN, 상한 DN, 허용굴곡각). 구간은 서로 겹치지 않는다.</summary>
    private static readonly (double MinDn, double MaxDn, double Deg)[] KpRanges =
    {
        (80, 150, 5.0), (200, 300, 4.0), (350, 500, 3.0), (600, 700, 2.0), (800, 1200, 1.5)
    };

    private static readonly (double MinDn, double MaxDn, double Deg)[] TytonRanges =
    {
        (80, 300, 5.0), (350, 400, 4.0), (450, 600, 3.0), (700, 900, 2.5), (1000, 1200, 2.0)
    };

    private static IEnumerable<JointDeflectionSpec> Build(string jointType, (double MinDn, double MaxDn, double Deg)[] ranges)
        => StraightPipeSpecTable.NominalDiameters
            .Select(dn => (Dn: dn, Deg: ranges.FirstOrDefault(r => dn >= r.MinDn && dn <= r.MaxDn).Deg))
            .Where(x => x.Deg > 0)
            .Select(x => new JointDeflectionSpec(jointType, x.Dn, x.Deg));

    public static JointDeflectionTable Default { get; } = new(
        Build(JointTypeCatalog.KpMechanical, KpRanges)
            .Concat(Build(JointTypeCatalog.Tyton, TytonRanges))
            .ToList());
}
