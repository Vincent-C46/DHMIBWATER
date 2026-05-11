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
        public string LevelName { get; set; } = string.Empty;
        public double Height { get; set; }
        public double BaseOffset { get; set; }
        public bool IsFlipped { get; set; } = false;
        
        public string ElementCode { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public string Part { get; set; } = string.Empty;

        public Curve3D WallCurve
        {
            get { return new Line3D(StartPoint, EndPoint); }
        }
    }
}
