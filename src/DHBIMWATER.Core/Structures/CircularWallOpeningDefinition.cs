using DHBIMWATER.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Core.Structures
{
    public record CircularWallOpeningDefinition
    {
        public double Diameter { get; set; }
        public Point3D Position { get; set; }

        public string Name { get; set; } = string.Empty;
        public string LevelName { get; set; } = string.Empty;
        public string HostElementCode { get; set; } = string.Empty;
        public double OffsetZ { get; set; } = 0.0;
        public string Category { get; set; } = "개구부";
        public string ElementCode { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public string Part { get; set; } = string.Empty;
    }
}
