using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Core.Geometry
{
    public static class PolygonCalculator
    {
        public static double ComputeArea(IReadOnlyList<Point2D> points)
        {
            int n = points.Count;
            double area = 0;

            for (int i = 0; i < n; i++)
            {
                var currentPt = points[i];
                var nextPt = points[(i + 1) % n];
                area += currentPt.X * nextPt.Y - nextPt.X * currentPt.Y;
            }

            return Math.Abs(area) / 2.0;
        }

        public static double ComputePerimeter(IReadOnlyList<Point2D> points)
        {
            int n = points.Count;
            double perimeter = 0;
            for (int i = 0; i < n; i++)
            {
                var currentPt = points[i];
                var nextPt = points[(i + 1) % n];
                perimeter += currentPt.DistanceTo(nextPt);
            }
            return perimeter;
        }
    }
}
