namespace DHBIMWATER.Core.Geometry
{
    public static class ConvexHull
    {
        // Andrew's Monotone Chain — O(n log n), 반시계 방향 반환
        public static IReadOnlyList<Point2D> Compute(IEnumerable<Point2D> points)
        {
            var pts = points.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
            int n = pts.Count;
            if (n < 3) return pts;

            var hull = new List<Point2D>();

            // Lower hull
            foreach (var p in pts)
            {
                while (hull.Count >= 2 && Cross(hull[^2], hull[^1], p) <= 0)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(p);
            }

            // Upper hull
            int lower = hull.Count + 1;
            for (int i = n - 2; i >= 0; i--)
            {
                while (hull.Count >= lower && Cross(hull[^2], hull[^1], pts[i]) <= 0)
                    hull.RemoveAt(hull.Count - 1);
                hull.Add(pts[i]);
            }

            hull.RemoveAt(hull.Count - 1);
            return hull;
        }

        public static bool IsOnBoundary(Point2D point, IReadOnlyList<Point2D> hull, double tolerance)
        {
            int n = hull.Count;
            for (int i = 0; i < n; i++)
            {
                if (DistanceToSegment(point, hull[i], hull[(i + 1) % n]) <= tolerance)
                    return true;
            }
            return false;
        }

        private static double Cross(Point2D o, Point2D a, Point2D b)
            => (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);

        private static double DistanceToSegment(Point2D p, Point2D a, Point2D b)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y;
            if (dx == 0 && dy == 0) return p.DistanceTo(a);
            double t = Math.Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dy) / (dx * dx + dy * dy), 0, 1);
            return p.DistanceTo(new Point2D(a.X + t * dx, a.Y + t * dy));
        }
    }
}
