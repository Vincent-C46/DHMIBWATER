using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Core.Geometry
{
    public class Line3D : Curve3D
    {
        public override Point3D StartPoint { get; }
        public override Point3D EndPoint { get; }
        public override double Length => StartPoint.DistanceTo(EndPoint);

        public Line3D(Point3D startPoint, Point3D endPoint)
        {
            StartPoint = startPoint;
            EndPoint = endPoint;
        }
    }
}