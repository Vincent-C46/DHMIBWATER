using DHBIMWATER.Core.Geometry;

namespace DHBIMWATER.Core.Piping;

public enum SegmentIntersectionKind { None, Point, Overlap }

public readonly record struct SegmentIntersection(SegmentIntersectionKind Kind, Point2D? Point, double TOnFirst, double TOnSecond);

/// <summary>허용오차를 고려한 2D 선분 교차 계산이다.</summary>
public static class Segment2D
{
    private const double AngularTolerance = 1e-12;

    public static SegmentIntersection Intersect(Point2D a, Point2D b, Point2D c, Point2D d, double tolerance)
    {
        var rx = b.X - a.X; var ry = b.Y - a.Y;
        var sx = d.X - c.X; var sy = d.Y - c.Y;
        var rLength = Math.Sqrt(rx * rx + ry * ry);
        var sLength = Math.Sqrt(sx * sx + sy * sy);
        if (rLength <= double.Epsilon || sLength <= double.Epsilon)
            return new(SegmentIntersectionKind.None, null, 0, 0);

        var qpx = c.X - a.X; var qpy = c.Y - a.Y;
        var cross = Cross(rx, ry, sx, sy);
        var qpxr = Cross(qpx, qpy, rx, ry);

        // 외적은 길이², tolerance는 길이 단위이므로 직접 비교하지 않는다.
        // 평행 여부는 무차원 각도 오차로, 공선 여부는 실제 수선거리로 판정한다.
        if (Math.Abs(cross) / (rLength * sLength) <= AngularTolerance)
        {
            if (Math.Abs(qpxr) / rLength > tolerance)
                return new(SegmentIntersectionKind.None, null, 0, 0);

            var rLengthSquared = rx * rx + ry * ry;
            var t0 = (qpx * rx + qpy * ry) / rLengthSquared;
            var t1 = t0 + (sx * rx + sy * ry) / rLengthSquared;
            var overlapStart = Math.Max(0, Math.Min(t0, t1));
            var overlapEnd = Math.Min(1, Math.Max(t0, t1));
            var tTolerance = tolerance / rLength;
            if (overlapEnd < overlapStart - tTolerance)
                return new(SegmentIntersectionKind.None, null, 0, 0);

            if (Math.Abs(overlapEnd - overlapStart) <= tTolerance)
            {
                var contactT = Math.Clamp((overlapStart + overlapEnd) / 2, 0, 1);
                var point = new Point2D(a.X + contactT * rx, a.Y + contactT * ry);
                var contactU = Math.Clamp(ParameterOnSegment(point, c, d), 0, 1);
                return new(SegmentIntersectionKind.Point, point, contactT, contactU);
            }

            // 실제 선분 범위가 겹칠 때만 Overlap을 반환한다.
            return new(SegmentIntersectionKind.Overlap, null, 0, 0);
        }

        var t = Cross(qpx, qpy, sx, sy) / cross;
        var u = Cross(qpx, qpy, rx, ry) / cross;
        var tParameterTolerance = tolerance / rLength;
        var uParameterTolerance = tolerance / sLength;
        if (t < -tParameterTolerance || t > 1 + tParameterTolerance ||
            u < -uParameterTolerance || u > 1 + uParameterTolerance)
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
