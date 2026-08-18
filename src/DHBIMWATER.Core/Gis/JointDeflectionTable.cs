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

    // TODO: 주철관 핸드북 수치 미확보
    public static JointDeflectionTable Default { get; } = new(Array.Empty<JointDeflectionSpec>());
}
