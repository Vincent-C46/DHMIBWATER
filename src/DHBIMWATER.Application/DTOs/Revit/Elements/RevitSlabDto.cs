using DHBIMWATER.Application.DTOs.Common;
using DHBIMWATER.Application.DTOs.Geometry;

namespace DHBIMWATER.Application.DTOs.Revit.Elements
{
    /// <summary>
    /// 배수지 슬래브(바닥/지붕) 정보를 담는 DTO
    /// </summary>
    public class RevitSlabDto
    {
        /// <summary>
        /// 요소 ID
        /// </summary>
        public int ElementId { get; set; }

        /// <summary>
        /// 슬래브 이름
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 슬래브 타입 이름
        /// </summary>
        public string SlabTypeName { get; set; } = string.Empty;

        /// <summary>
        /// 슬래브 유형 (바닥/지붕)
        /// </summary>
        public string SlabCategory { get; set; } = string.Empty;

        /// <summary>
        /// 슬래브 두께 (미터)
        /// </summary>
        public double Thickness { get; set; }

        /// <summary>
        /// 슬래브 면적 (제곱미터)
        /// </summary>
        public double Area { get; set; }

        /// <summary>
        /// 슬래브 체적 (세제곱미터)
        /// </summary>
        public double Volume { get; set; }

        /// <summary>
        /// 둘레 길이 (미터)
        /// </summary>
        public double Perimeter { get; set; }

        /// <summary>
        /// 레벨 이름
        /// </summary>
        public string? LevelName { get; set; }

        /// <summary>
        /// 레벨로부터의 오프셋 (미터)
        /// </summary>
        public double OffsetFromLevel { get; set; }

        /// <summary>
        /// 중심점
        /// </summary>
        public Point3DDto CenterPoint { get; set; } = new();

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
