using DHBIMWATER.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Core.Structures
{
    public class BeamDefinition
    {
        public Point3D StartPoint { get; set; }
        public Point3D EndPoint { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string LevelName { get; init; } = string.Empty;

        public int Zjustification { get; set; } = 2; // Z맞춤: 상단(0), 중심(1), 원점 (2), 하단(3)
        public string ElementCode { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public string Part { get; set; } = string.Empty;
    }
}
