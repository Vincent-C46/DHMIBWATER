using DHBIMWATER.Core.Geometry;

namespace DHBIMWATER.Core.Structures
{
    public class StairsRunDefinition
    {
        public Point3D StartPoint { get; set; } // 직선 Run 이동선 시작점
        public Point3D EndPoint { get; set; }   // 직선 Run 이동선 끝점
        public StairJustification Justification { get; set; } = StairJustification.Center;
        public double Width { get; set; } // mm, Run 폭 (0이면 미반영 - StairsType 기본값 사용). StairsRun.ActualRunWidth에 대응
    }
}
