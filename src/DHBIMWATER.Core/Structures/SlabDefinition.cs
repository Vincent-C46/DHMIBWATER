using DHBIMWATER.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Core.Structures
{
    public record SlabDefinition
    {
        public IReadOnlyList<Point2D> Points {get; set;}
        public IReadOnlyList<Point2D> SubPoints {get; set; }    // 프로파일 추가용 - 오프닝이 아닌 경우 빈 리스트
        public double Thickness { get; set; }
        public double ElevationZ { get; set; }
        public string LevelName { get; init; } = string.Empty;

        public string ElementCode { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public string Part { get; set; } = string.Empty;

        public double Area => PolygonCalculator.ComputeArea(Points) - PolygonCalculator.ComputeArea(SubPoints);
        public double Perimeter => PolygonCalculator.ComputePerimeter(Points);
        public double Volume => Area * Thickness;
    }
}
