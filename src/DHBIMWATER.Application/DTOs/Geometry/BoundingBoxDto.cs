using DHBIMWATER.Application.DTOs.Common;

namespace DHBIMWATER.Application.DTOs.Geometry
{
    /// <summary>
    /// 3D 경계 상자 정보를 담는 DTO
    /// </summary>
    public class BoundingBoxDto
    {
        /// <summary>
        /// 최소 점 (왼쪽 하단 앞)
        /// </summary>
        public Point3DDto MinPoint { get; set; } = new();

        /// <summary>
        /// 최대 점 (오른쪽 상단 뒤)
        /// </summary>
        public Point3DDto MaxPoint { get; set; } = new();

        /// <summary>
        /// 너비 (X 방향, 미터)
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// 높이 (Z 방향, 미터)
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// 깊이 (Y 방향, 미터)
        /// </summary>
        public double Depth { get; set; }

        /// <summary>
        /// 중심점
        /// </summary>
        public Point3DDto CenterPoint { get; set; } = new();
    }
}