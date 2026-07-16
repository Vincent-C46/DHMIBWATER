using DHBIMWATER.Core.Geometry;

namespace DHBIMWATER.Core.Piping;

public enum SegmentIntersectionKind { None, Point, Overlap }

public readonly record struct SegmentIntersection(SegmentIntersectionKind Kind, Point2D? Point, double TOnFirst, double TOnSecond);

/// <summary>허용오차를 고려한 2D 선분 교차 계산이다.</summary>
public static class Segment2D
{
    public static SegmentIntersection Intersect(Point2D a, Point2D b, Point2D c, Point2D d, double tolerance)
    {
        var rx = b.X - a.X; var ry = b.Y - a.Y;
        var sx = d.X - c.X; var sy = d.Y - c.Y;
        var qpx = c.X - a.X; var qpy = c.Y - a.Y;
        var cross = Cross(rx, ry, sx, sy);
        var qpxr = Cross(qpx, qpy, rx, ry);

        if (Math.Abs(cross) <= tolerance)
        {
            if (Math.Abs(qpxr) > tolerance) return new(SegmentIntersectionKind.None, null, 0, 0);
            // 겹침은 AddSegment에서 기존 끝점들을 분할점으로 투영해 정규화한다.
            return new(SegmentIntersectionKind.Overlap, null, 0, 0);
        }

        var t = Cross(qpx, qpy, sx, sy) / cross;
        var u = Cross(qpx, qpy, rx, ry) / cross;
        if (t < -tolerance || t > 1 + tolerance || u < -tolerance || u > 1 + tolerance)
            return new(SegmentIntersectionKind.None, null, 0, 0);

        t = Math.Clamp(t, 0, 1); u = Math.Clamp(u, 0, 1);
        return new(SegmentIntersectionKind.Point, new Point2D(a.X + t * rx, a.Y + t * ry), t, u);
    }

    public static double ParameterOnSegment(Point2D point, Point2D start, Point2D end)
    {
        var dx = end.X - start.X; var dy = end.Y - start.Y;
        var lengthSquared = dx * dx + dy * dy;
        return lengthSquared <= double.Epsilon ? 0 : ((point.X - start.X) * dx + (point.Y - start.Y) * dy) / lengthSquared;
    }

    private static double Cross(double ax, double ay, double bx, double by) => ax * by - ay * bx;
}
