using DHBIMWATER.Application.DTOs.Common;
using DHBIMWATER.Application.DTOs.Geometry;

namespace DHBIMWATER.Application.DTOs.Revit.Reservoir
{
    /// <summary>
    /// 배수지 벽체 정보를 담는 DTO
    /// </summary>
    public class ReservoirWallDto
    {
        /// <summary>
        /// 요소 ID
        /// </summary>
        public int ElementId { get; set; }

        /// <summary>
        /// 벽체 이름
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 벽 타입 이름
        /// </summary>
        public string WallTypeName { get; set; } = string.Empty;

        /// <summary>
        /// 벽 높이 (미터)
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// 벽 두께 (미터)
        /// </summary>
        public double Thickness { get; set; }

        /// <summary>
        /// 벽 길이 (미터)
        /// </summary>
        public double Length { get; set; }

        /// <summary>
        /// 벽 면적 (제곱미터)
        /// </summary>
        public double Area { get; set; }

        /// <summary>
        /// 벽 체적 (세제곱미터)
        /// </summary>
        public double Volume { get; set; }

        /// <summary>
        /// 시작점
        /// </summary>
        public Point3DDto StartPoint { get; set; } = new();

        /// <summary>
        /// 끝점
        /// </summary>
        public Point3DDto EndPoint { get; set; } = new();

        /// <summary>
        /// 레벨 이름
        /// </summary>
        public string? LevelName { get; set; }

        /// <summary>
        /// 재료 정보
        /// </summary>
        public string? MaterialInfo { get; set; }

        /// <summary>
        /// 경계 상자
        /// </summary>
        public BoundingBoxDto? BoundingBox { get; set; }
    }
}
