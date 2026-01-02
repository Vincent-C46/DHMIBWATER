using DHBIMWATER.Application.DTOs.Common;
using DHBIMWATER.Application.DTOs.Geometry;

namespace DHBIMWATER.Application.DTOs.Revit.Reservoir
{
    /// <summary>
    /// 배수지 기둥 정보를 담는 DTO
    /// </summary>
    public class ReservoirColumnDto
    {
        /// <summary>
        /// 요소 ID
        /// </summary>
        public int ElementId { get; set; }

        /// <summary>
        /// 기둥 이름
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 기둥 타입 이름
        /// </summary>
        public string ColumnTypeName { get; set; } = string.Empty;

        /// <summary>
        /// 기둥 높이 (미터)
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// 단면 너비 (미터)
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// 단면 깊이 (미터)
        /// </summary>
        public double Depth { get; set; }

        /// <summary>
        /// 기둥 체적 (세제곱미터)
        /// </summary>
        public double Volume { get; set; }

        /// <summary>
        /// 위치 (하단 중심점)
        /// </summary>
        public Point3DDto Location { get; set; } = new();

        /// <summary>
        /// 회전 각도 (라디안)
        /// </summary>
        public double Rotation { get; set; }

        /// <summary>
        /// 베이스 레벨 이름
        /// </summary>
        public string? BaseLevelName { get; set; }

        /// <summary>
        /// 상단 레벨 이름
        /// </summary>
        public string? TopLevelName { get; set; }

        /// <summary>
        /// 베이스 오프셋 (미터)
        /// </summary>
        public double BaseOffset { get; set; }

        /// <summary>
        /// 상단 오프셋 (미터)
        /// </summary>
        public double TopOffset { get; set; }

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
