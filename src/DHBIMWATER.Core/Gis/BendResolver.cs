namespace DHBIMWATER.Core.Gis;

public enum BendResolutionKind
{
    /// <summary>편각이 허용굴곡 안 — 곡관 없이 직관 편차로 흡수한다.</summary>
    None,
    /// <summary>표준 곡관 1개로 해결된다.</summary>
    Standard,
    /// <summary>어느 표준각의 허용범위에도 들지 않는다. 곡관을 넣지 않고 사용자 확인 대상으로 남긴다.</summary>
    Unresolved
}

/// <param name="StandardAngleDeg">Standard면 선정된 표준각, Unresolved면 가장 가까운 표준각, None이면 0.</param>
/// <param name="ResidualDeg">편각과 StandardAngleDeg의 차(부호 포함). None이면 편각 그대로.</param>
/// <param name="LayingLengthMm">
/// t — Standard일 때 선정된 곡관의 절점~관 끝 거리(mm). None/Unresolved/치수 미입력이면 0.
/// 배치 단계에서 이 값만큼 양쪽 직관 구간을 줄이고 그 지점부터 다시 interval로 분절한다.
/// </param>
/// <param name="CenterlineRadiusMm">R — 중심선 호의 곡률반경(mm). 호 중점 P2 계산에 쓴다. 치수 미입력이면 0.</param>
/// <param name="TangentLengthMm">T = R·tan(θ/2). 계산값이며 입력값이 아니다.</param>
/// <param name="HasFittingSize">카탈로그에서 t·R을 찾았는지. false면 직관 차감도 호 계산도 할 수 없다.</param>
public sealed record BendResolution(
    int NodeId,
    BendResolutionKind Kind,
    double DeflectionDeg,
    double StandardAngleDeg,
    double ToleranceDeg,
    double ResidualDeg,
    double LayingLengthMm,
    double CenterlineRadiusMm,
    double TangentLengthMm,
    bool HasFittingSize)
{
    /// <summary>
    /// t &lt; T면 호가 곡관 몸통 밖으로 나가므로 치수가 성립하지 않는다.
    /// 사용자 결정(2026-07-31)에 따라 모델링은 진행하되 호출부에서 경고를 띄운다.
    /// </summary>
    public bool IsSizeConsistent => !HasFittingSize || LayingLengthMm + 1e-9 >= TangentLengthMm;
}

/// <summary>편각과 허용굴곡 설정으로 절점에 들어갈 곡관을 판정한다. Revit 의존 없는 순수 계산이다.</summary>
public static class BendResolver
{
    // TODO: 곡관 조합 해법 미정 (PROGRESS.md 참조) — 표준각 어디에도 안 맞는 편각은 Unresolved로만 표시한다.

    public static BendResolution Resolve(NodeClassification node, BendSettings settings, BendForm form)
    {
        if (node.Kind != NodeKind.Bend)
            throw new ArgumentException("곡관 판정은 Bend 절점에서만 정의된다.", nameof(node));

        var theta = node.DeflectionDeg;
        var tolerance = settings.Tolerance.ToleranceFor(node.PipeKind, node.MaxDiameterMm);

        if (theta <= tolerance)
            return new BendResolution(node.NodeId, BendResolutionKind.None, theta, 0d, tolerance, theta, 0d, 0d, 0d, false);

        // 가장 가까운 표준각. 동률(정확히 중간)이면 작은 각을 고른다 — StandardAngles가 오름차순이고 비교가 strict라서 유지된다.
        var nearest = BendToleranceTable.StandardAngles[0];
        foreach (var angle in BendToleranceTable.StandardAngles)
            if (Math.Abs(theta - angle) < Math.Abs(theta - nearest)) nearest = angle;

        var residual = theta - nearest;
        if (Math.Abs(residual) > tolerance)
            return new BendResolution(node.NodeId, BendResolutionKind.Unresolved, theta, nearest, tolerance, residual, 0d, 0d, 0d, false);

        var fitting = settings.Fittings.Find(node.PipeKind, node.MaxDiameterMm, nearest, form);
        if (fitting is null)
            return new BendResolution(node.NodeId, BendResolutionKind.Standard, theta, nearest, tolerance, residual, 0d, 0d, 0d, false);

        var tangent = TangentLength(fitting.CenterlineRadiusMm, nearest);
        return new BendResolution(
            node.NodeId, BendResolutionKind.Standard, theta, nearest, tolerance, residual,
            fitting.LayingLengthMm, fitting.CenterlineRadiusMm, tangent, true);
    }

    public static IReadOnlyList<BendResolution> ResolveAll(IReadOnlyList<NodeClassification> nodes, BendSettings settings, BendForm form)
        => nodes.Where(x => x.Kind == NodeKind.Bend).Select(x => Resolve(x, settings, form)).ToList();

    /// <summary>접선길이 T = R·tan(θ/2). 절점에서 호의 시작점까지의 거리다.</summary>
    public static double TangentLength(double radiusMm, double angleDeg)
        => radiusMm * Math.Tan(angleDeg * Math.PI / 360d);
}
