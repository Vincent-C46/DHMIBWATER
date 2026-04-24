using DHBIMWATER.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Core.Structures
{
    public record ProfileWallDefinition
    {
        public IReadOnlyList<Point3D> Points { get; set; }
        public double Thickness { get; set; }
        public string LevelName { get; set; } = string.Empty;
        public bool IsFlipped { get; set; } = false;

        public string ElementCode { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public string Part { get; set; } = string.Empty;
    }
}