using DHBIMWATER.Core.Geometry;

namespace DHBIMWATER.Core.Structures
{
    public class StairsLandingDefinition
    {
        // 층계참 외곽 폐곡선 (각 점의 Z가 층계참 높이). 3개 이상의 점 필요.
        public List<Point3D> BoundaryPoints { get; set; } = new List<Point3D>();
    }
}
