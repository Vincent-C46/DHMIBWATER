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
        public string LevelName { get; init; } = string.Empty;
        public double Height { get; set; }       // 기둥 높이 (mm)
        public string Category { get; set; } = "기둥";
        public string ElementCode { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public string Part { get; set; } = string.Empty;
    }
}
