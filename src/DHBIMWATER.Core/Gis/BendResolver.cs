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

/// <summary>
/// 절점의 관종을 보고 <see cref="IBendingRule"/>을 골라 곡관 판정을 위임한다.
/// 판정 로직 자체는 관종별 규칙(덕타일은 <see cref="JointDeflectionRule"/>)이 갖는다.
/// </summary>
public static class BendResolver
{
    /// <summary>덕타일 표준 곡관 각도. 관종별 규칙으로 옮겼고 기존 호출부를 위해 남겨둔다.</summary>
    public static IReadOnlyList<double> StandardAngles => JointDeflectionRule.StandardAngles;

    /// <summary>표에서 유효 허용굴곡을 조회한다. 판정 계산은 하지 않는다.</summary>
    public static double? GetAllowableDeflection(JointDeflectionTable table, string jointType, double dn, JointApplicationMode mode)
        => JointDeflectionRule.GetAllowableDeflection(table, jointType, dn, mode);

    /// <summary>선정 곡관의 잔여각이 유효 허용굴곡 안인지 계산한다.</summary>
    public static bool EvaluateFitting(double actualAngleDeg, double fittingAngleDeg, double effectiveAllowableDeg)
        => JointDeflectionRule.EvaluateFitting(actualAngleDeg, fittingAngleDeg, effectiveAllowableDeg);

    public static BendResolution Resolve(NodeClassification node, BendSettings settings, BendForm form)
        => BendingRuleCatalog.For(MaterialOf(node)).Resolve(node, settings, form);

    public static IReadOnlyList<BendResolution> ResolveAll(IReadOnlyList<NodeClassification> nodes, BendSettings settings, BendForm form)
        => nodes.Where(x => x.Kind == NodeKind.Bend).Select(x => Resolve(x, settings, form)).ToList();

    public static double TangentLength(double radiusMm, double angleDeg)
        => radiusMm * Math.Tan(angleDeg * Math.PI / 360d);

    /// <summary>
    /// 알 수 없거나 비어 있는 등급명은 덕타일로 본다. 관종 축 도입 전과 동일하게 동작시키기 위한 것이며,
    /// 알 수 없는 등급 자체는 이미 <c>AlignmentSourceLoader</c>가 경고로 보고한다.
    /// </summary>
    private static PipeMaterial MaterialOf(NodeClassification node)
        => PipeKindCatalog.MaterialOf(node.PipeKind) ?? PipeMaterial.DuctileIron;
}
