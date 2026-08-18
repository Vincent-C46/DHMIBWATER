namespace DHBIMWATER.Core.Gis;

public enum BendResolutionKind
{
    /// <summary>곡관 불필요(Joint 허용굴곡 내).</summary>
    None,
    /// <summary>표준 곡관과 Joint 허용굴곡으로 해결된다.</summary>
    Standard,
    /// <summary>최근접 표준 곡관을 배치하되 허용 초과로 표시한다.</summary>
    Unresolved
}

public sealed record BendResolution(
    int NodeId,
    BendResolutionKind Kind,
    double DeflectionDeg,
    double StandardAngleDeg,
    double AllowableDeg,
    double ResidualDeg,
    double LayingLengthMm,
    double CenterlineRadiusMm,
    double TangentLengthMm,
    bool HasFittingSize,
    string JointType,
    JointApplicationMode ApplicationMode,
    double EffectiveAllowableDeg,
    bool IsAcceptable,
    double ExtraLegLengthMm = 0d,
    double WallThicknessMm = 0d,
    string? TypeName = null)
{
    public double LongLegLengthMm => LayingLengthMm > 0d ? LayingLengthMm + ExtraLegLengthMm : 0d;
    public bool IsSizeConsistent => !HasFittingSize || LayingLengthMm + 1e-9 >= TangentLengthMm;
}

/// <summary>편각과 Joint 허용굴곡 설정으로 절점에 들어갈 곡관을 판정한다.</summary>
public static class BendResolver
{
    public static IReadOnlyList<double> StandardAngles { get; } = new[] { 11.25, 22.5, 45d, 90d };

    /// <summary>표에서 유효 허용굴곡을 조회한다. 판정 계산은 하지 않는다.</summary>
    public static double? GetAllowableDeflection(JointDeflectionTable table, string jointType, double dn, JointApplicationMode mode)
        => table.EffectiveAllowableFor(jointType, dn, mode);

    /// <summary>선정 곡관의 잔여각이 유효 허용굴곡 안인지 계산한다.</summary>
    public static bool EvaluateFitting(double actualAngleDeg, double fittingAngleDeg, double effectiveAllowableDeg)
        => Math.Abs(actualAngleDeg - fittingAngleDeg) <= effectiveAllowableDeg + 1e-9;

    public static BendResolution Resolve(NodeClassification node, BendSettings settings, BendForm form)
    {
        if (node.Kind != NodeKind.Bend)
            throw new ArgumentException("곡관 판정은 Bend 절점에서만 정의된다.", nameof(node));

        var actual = node.DeflectionDeg;
        var jointType = settings.ActiveJointType;
        var singleAllowable = settings.JointDeflections.AllowableFor(jointType, node.MaxDiameterMm) ?? 0d;
        var effectiveValue = GetAllowableDeflection(settings.JointDeflections, jointType, node.MaxDiameterMm, settings.ApplicationMode);
        var hasDeflectionSetting = effectiveValue.HasValue;
        var effective = effectiveValue ?? 0d;

        if (hasDeflectionSetting && actual <= effective + 1e-9)
            return Empty(node, BendResolutionKind.None, actual, 0d, singleAllowable, actual, jointType, settings.ApplicationMode, effective, true);

        var acceptable = StandardAngles
            .Select(angle => new { Angle = angle, Residual = Math.Abs(actual - angle) })
            .Where(x => hasDeflectionSetting && EvaluateFitting(actual, x.Angle, effective))
            .OrderBy(x => x.Residual)
            .ThenBy(x => x.Angle)
            .FirstOrDefault();

        var selectedAngle = acceptable?.Angle ?? StandardAngles
            .OrderBy(angle => Math.Abs(actual - angle))
            .ThenBy(angle => angle)
            .First();
        var isAcceptable = acceptable is not null;
        var kind = isAcceptable ? BendResolutionKind.Standard : BendResolutionKind.Unresolved;
        var residual = actual - selectedAngle;
        var fitting = settings.Fittings.Find(node.MaxDiameterMm, selectedAngle, form);
        if (fitting is null)
            return Empty(node, kind, actual, selectedAngle, singleAllowable, residual, jointType, settings.ApplicationMode, effective, isAcceptable);

        var tangent = TangentLength(fitting.CenterlineRadiusMm, selectedAngle);
        return new BendResolution(
            node.NodeId, kind, actual, selectedAngle, singleAllowable, residual,
            fitting.LayingLengthMm, fitting.CenterlineRadiusMm, tangent, true,
            jointType, settings.ApplicationMode, effective, isAcceptable,
            fitting.ExtraLegLengthMm, fitting.WallThicknessMm, fitting.TypeName);
    }

    public static IReadOnlyList<BendResolution> ResolveAll(IReadOnlyList<NodeClassification> nodes, BendSettings settings, BendForm form)
        => nodes.Where(x => x.Kind == NodeKind.Bend).Select(x => Resolve(x, settings, form)).ToList();

    public static double TangentLength(double radiusMm, double angleDeg)
        => radiusMm * Math.Tan(angleDeg * Math.PI / 360d);

    private static BendResolution Empty(NodeClassification node, BendResolutionKind kind, double actual, double standardAngle,
        double allowable, double residual, string jointType, JointApplicationMode mode, double effective, bool isAcceptable)
        => new(node.NodeId, kind, actual, standardAngle, allowable, residual, 0d, 0d, 0d, false,
            jointType, mode, effective, isAcceptable);
}
