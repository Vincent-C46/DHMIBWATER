using DHBIMWATER.Core.Geometry;

namespace DHBIMWATER.Core.Piping;

/// <summary>절점에서 뻗어 나가는 레그 하나. 방향은 절점에서 바깥을 향하는 단위벡터다.</summary>
public readonly record struct PipeNodeLeg(PipeEdgeDefinition Edge, double DirectionX, double DirectionY);

/// <summary>
/// 절점의 꺾임각과 레그 방향을 계산한다(docs/39 §4.1).
/// </summary>
public static class PipeNodeAngle
{
    /// <summary>각도 판정 허용오차(도). <see cref="PipeTeeResolver.DefaultAngleToleranceDeg"/>와 같은 값을 쓴다.</summary>
    public const double AngleToleranceDeg = PipeTeeResolver.DefaultAngleToleranceDeg;
    private const double PositionToleranceMm = 0.01;

    /// <summary>절점에 붙은 레그 목록. 길이가 0인 엣지는 방향을 구할 수 없어 제외한다.</summary>
    public static IReadOnlyList<PipeNodeLeg> Legs(PipeNodeDefinition node, IReadOnlyList<PipeEdgeDefinition> edges)
    {
        var legs = new List<PipeNodeLeg>();
        foreach (var edge in edges)
        {
            if (!IsAt(edge.Start, node.Position) && !IsAt(edge.End, node.Position)) continue;
            var other = IsAt(edge.Start, node.Position) ? edge.End : edge.Start;
            var length = node.Position.DistanceTo(other);
            if (length <= double.Epsilon) continue;
            legs.Add(new PipeNodeLeg(edge, (other.X - node.Position.X) / length, (other.Y - node.Position.Y) / length));
        }
        return legs;
    }

    /// <summary>절점의 꺾임각(도). 두 레그가 일직선이면 0, 직각으로 꺾이면 90이다.
    /// 레그가 2개가 아니면 null(곡관 대상이 아니다).</summary>
    public static double? Deflection(PipeNodeDefinition node, IReadOnlyList<PipeEdgeDefinition> edges)
    {
        var legs = Legs(node, edges);
        if (legs.Count != 2) return null;
        var dot = Math.Clamp(legs[0].DirectionX * legs[1].DirectionX + legs[0].DirectionY * legs[1].DirectionY, -1, 1);
        return 180 - Math.Acos(dot) * 180 / Math.PI;
    }

    private static bool Within(double? value, double target)
        => value is not null && Math.Abs(value.Value - target) <= AngleToleranceDeg;

    private static bool IsAt(Point2D point, Point2D node) => point.DistanceTo(node) <= PositionToleranceMm;
}
