using DHBIMWATER.Application.DTOs.Common;

namespace DHBIMWATER.Application.DTOs.Geometry
{
    /// <summary>
    /// 선/곡선 정보를 담는 DTO
    /// </summary>
    public class CurveDto
    {
        /// <summary>
        /// 시작점
        /// </summary>
        public Point3DDto StartPoint { get; set; } = new();

        /// <summary>
        /// 끝점
        /// </summary>
        public Point3DDto EndPoint { get; set; } = new();

        /// <summary>
        /// 곡선 길이 (미터)
        /// </summary>
        public double Length { get; set; }

        /// <summary>
        /// 곡선 타입 (Line, Arc, Ellipse 등)
        /// </summary>
        public string CurveType { get; set; } = string.Empty;

        /// <summary>
        /// 호인 경우 중심점
        /// </summary>
        public Point3DDto? ArcCenter { get; set; }

        /// <summary>
        /// 호인 경우 반지름 (미터)
        /// </summary>
        public double? ArcRadius { get; set; }

        /// <summary>
        /// 호인 경우 각도 (라디안)
        /// </summary>
        public double? ArcAngle { get; set; }
    }
}
