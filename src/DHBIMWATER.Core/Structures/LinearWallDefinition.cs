using DHBIMWATER.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Core.Structures
{
    public record LinearWallDefinition
    {
        public Point3D StartPoint { get; set; }
        public Point3D EndPoint { get; set; }
        public double Thickness { get; set; }
        public double ElevationZ { get; set; }
        public double Height { get; set; }

        public Curve3D WallCurve
        {
            get { return new Line3D(StartPoint, EndPoint); }
        }
    }
}
