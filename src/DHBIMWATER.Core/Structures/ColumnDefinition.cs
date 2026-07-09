using DHBIMWATER.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Core.Structures
{
    public class ColumnDefinition
    {
        public Point3D Position { get; set; }   // 기둥 중심점 (mm)
        public string TypeName { get; set; } = string.Empty; // Revit 패밀리 타입명
        public string BaseLevelName { get; init; } = string.Empty;
        public string TopLevelName { get; init; } = string.Empty;
        public double BaseOffset { get; set; }
        public double TopOffset { get; set; }
        public string ElementCode { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public string Part { get; set; } = string.Empty;
    }
}
