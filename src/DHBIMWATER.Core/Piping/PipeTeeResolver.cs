using DHBIMWATER.Core.Geometry;

namespace DHBIMWATER.Core.Piping;

public sealed record PipeTeeLegOrder(Guid MainEdge1Id, Guid MainEdge2Id, Guid BranchEdgeId);

/// <summary>차수 3 절점의 두 공선 레그를 주관, 직교하는 나머지 레그를 분기관으로 판별한다.</summary>
public static class PipeTeeResolver
{
    public const double DefaultAngleToleranceDeg = 2.0;
    private const double PositionToleranceMm = 0.01;

    public static PipeTeeLegOrder? Resolve(
        PipeNodeDefinition node,
        IReadOnlyList<PipeEdgeDefinition> edges,
        double angleToleranceDeg = DefaultAngleToleranceDeg)
    {
        var legs = edges
            .Where(x => IsAt(x.Start, node.Position) || IsAt(x.End, node.Position))
            .Select(x => (Edge: x, Direction: DirectionAwayFrom(x, node.Position)))
            .Where(x => x.Direction is not null)
            .Select(x => (x.Edge, Direction: x.Direction!.Value))
            .ToList();
        if (legs.Count != 3) return null;

        var pairs = new[] { (A: 0, B: 1, Branch: 2), (A: 0, B: 2, Branch: 1), (A: 1, B: 2, Branch: 0) };
        var main = pairs.Select(x => (Pair: x, Angle: AngleDeg(legs[x.A].Direction, legs[x.B].Direction)))
            .OrderByDescending(x => x.Angle).First();
        if (Math.Abs(180 - main.Angle) > angleToleranceDeg) return null;

        var branchDirection = legs[main.Pair.Branch].Direction;
        if (Math.Abs(90 - AngleDeg(legs[main.Pair.A].Direction, branchDirection)) > angleToleranceDeg ||
            Math.Abs(90 - AngleDeg(legs[main.Pair.B].Direction, branchDirection)) > angleToleranceDeg)
            return null;

        return new PipeTeeLegOrder(
            legs[main.Pair.A].Edge.Id,
            legs[main.Pair.B].Edge.Id,
            legs[main.Pair.Branch].Edge.Id);
    }

    private static (double X, double Y)? DirectionAwayFrom(PipeEdgeDefinition edge, Point2D node)
    {
        var other = IsAt(edge.Start, node) ? edge.End : edge.Start;
        var length = node.DistanceTo(other);
        return length <= double.Epsilon ? null : ((other.X - node.X) / length, (other.Y - node.Y) / length);
    }

    private static double AngleDeg((double X, double Y) a, (double X, double Y) b)
        => Math.Acos(Math.Clamp(a.X * b.X + a.Y * b.Y, -1, 1)) * 180 / Math.PI;

    private static bool IsAt(Point2D point, Point2D node) => point.DistanceTo(node) <= PositionToleranceMm;
}
