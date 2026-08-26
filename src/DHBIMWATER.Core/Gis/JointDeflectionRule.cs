namespace DHBIMWATER.Core.Gis;

/// <summary>
/// 덕타일주철관 판정 규칙. 조인트 허용굴곡 안이면 곡관을 생략하고,
/// 넘으면 잔여각이 허용굴곡 안에 드는 표준 곡관을 고른다.
/// (기존 <c>BendResolver.Resolve</c> 본문을 관종별 규칙으로 옮긴 것으로, 판정 결과는 동일하다.)
/// </summary>
public sealed class JointDeflectionRule : IBendingRule
{
    private const double Epsilon = 1e-9;

    public PipeMaterial Material => PipeMaterial.DuctileIron;

    /// <summary>덕타일 표준 곡관 각도. 다른 관종은 이 목록을 쓰지 않는다.</summary>
    public static IReadOnlyList<double> StandardAngles { get; } = new[] { 11.25, 22.5, 45d, 90d };

    /// <summary>표에서 유효 허용굴곡을 조회한다. 판정 계산은 하지 않는다.</summary>
    public static double? GetAllowableDeflection(JointDeflectionTable table, string jointType, double dn, JointApplicationMode mode)
        => table.EffectiveAllowableFor(jointType, dn, mode);

    /// <summary>선정 곡관의 잔여각이 유효 허용굴곡 안인지 계산한다.</summary>
    public static bool EvaluateFitting(double actualAngleDeg, double fittingAngleDeg, double effectiveAllowableDeg)
        => Math.Abs(actualAngleDeg - fittingAngleDeg) <= effectiveAllowableDeg + Epsilon;

    public BendResolution Resolve(NodeClassification node, BendSettings settings)
    {
        if (node.Kind != NodeKind.Bend)
            throw new ArgumentException("곡관 판정은 Bend 절점에서만 정의된다.", nameof(node));

        var actual = node.DeflectionDeg;
        var jointType = settings.ActiveJointType;
        var singleAllowable = settings.JointDeflections.AllowableFor(jointType, node.MaxDiameterMm) ?? 0d;
        var effectiveValue = GetAllowableDeflection(settings.JointDeflections, jointType, node.MaxDiameterMm, settings.ApplicationMode);
        var hasDeflectionSetting = effectiveValue.HasValue;
        var effective = effectiveValue ?? 0d;

        if (hasDeflectionSetting && actual <= effective + Epsilon)
            return Empty(node, BendResolutionKind.None, actual, 0d, singleAllowable, actual, jointType, settings.ApplicationMode, effective, true);

        var availableAngles = BendFittingCatalog.AnglesFor(settings.ActiveBendConnection);
        var acceptable = availableAngles
            .Select(angle => new { Angle = angle, Residual = Math.Abs(actual - angle) })
            .Where(x => hasDeflectionSetting && EvaluateFitting(actual, x.Angle, effective))
            .OrderBy(x => x.Residual)
            .ThenBy(x => x.Angle)
            .FirstOrDefault();

        var selectedAngle = acceptable?.Angle ?? availableAngles
            .OrderBy(angle => Math.Abs(actual - angle))
            .ThenBy(angle => angle)
            .First();
        var isAcceptable = acceptable is not null;
        var kind = isAcceptable ? BendResolutionKind.Standard : BendResolutionKind.Unresolved;
        var residual = actual - selectedAngle;
        var fitting = settings.Fittings.Find(node.MaxDiameterMm, selectedAngle, Material, settings.ActiveBendConnection);
        if (fitting is null)
            return Empty(node, kind, actual, selectedAngle, singleAllowable, residual, jointType, settings.ApplicationMode, effective, isAcceptable);

        var tangent = BendResolver.TangentLength(fitting.CenterlineRadiusMm, selectedAngle);
        var weightKg = jointType == JointTypeCatalog.Tyton ? fitting.WeightTytonKg : fitting.WeightKpMechanicalKg;
        return new BendResolution(
            node.NodeId, kind, actual, selectedAngle, singleAllowable, residual,
            fitting.LayingLengthMm, fitting.CenterlineRadiusMm, tangent, true,
            jointType, settings.ApplicationMode, effective, isAcceptable,
            fitting.ExtraLegLengthMm, fitting.WallThicknessMm, fitting.Form, weightKg);
    }

    private static BendResolution Empty(NodeClassification node, BendResolutionKind kind, double actual, double standardAngle,
        double allowable, double residual, string jointType, JointApplicationMode mode, double effective, bool isAcceptable)
        => new(node.NodeId, kind, actual, standardAngle, allowable, residual, 0d, 0d, 0d, false,
            jointType, mode, effective, isAcceptable);
}
