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
        public string TypeName { get; set; } = string.Empty; // Revit 패밀리 타입명 (설정 시 우선 사용)
        public double Width { get; set; }   // TypeName이 없을 때 자동 유형 탐색용
        public double Height { get; set; }
        public string LevelName { get; init; } = string.Empty;

        public int ZJustification { get; set; } = 2; // Z맞춤: 상단(0), 중심(1), 원점 (2), 하단(3)
        public string Category { get; set; } = "보";
        public string ElementCode { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public string Part { get; set; } = string.Empty;
    }
}
