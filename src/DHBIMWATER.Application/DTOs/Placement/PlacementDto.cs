using DHBIMWATER.Application.DTOs.Common;

namespace DHBIMWATER.Application.DTOs.Placement
{
    /// <summary>
    /// Revit 요소의 배치 정보를 담는 DTO
    /// </summary>
    public class PlacementDto
    {
        /// <summary>
        /// 배치 위치 (X, Y, Z)
        /// </summary>
        public Point3DDto Location { get; set; } = new();

        /// <summary>
        /// 회전 각도 (라디안)
        /// </summary>
        public double Rotation { get; set; }

        /// <summary>
        /// 레벨 이름
        /// </summary>
        public string? LevelName { get; set; }

        /// <summary>
        /// 레벨로부터의 오프셋 (미터)
        /// </summary>
        public double OffsetFromLevel { get; set; }

        /// <summary>
        /// X축 회전 (라디안)
        /// </summary>
        public double RotationX { get; set; }

        /// <summary>
        /// Y축 회전 (라디안)
        /// </summary>
        public double RotationY { get; set; }

        /// <summary>
        /// Z축 회전 (라디안)
        /// </summary>
        public double RotationZ { get; set; }

        /// <summary>
        /// 뒤집기 여부 (손/발)
        /// </summary>
        public bool IsFlipped { get; set; }

        /// <summary>
        /// 작업 평면 기반 배치 여부
        /// </summary>
        public bool IsWorkPlaneBased { get; set; }
    }
}
