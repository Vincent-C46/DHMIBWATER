using DHBIMWATER.Application.DTOs.Common;

namespace DHBIMWATER.Application.DTOs.Revit
{
    public class GenericModelFamilyDto
    {
        /// <summary>
        /// 패밀리 이름
        /// </summary>
        public string FamilyName { get; set; } = string.Empty;

        /// <summary>
        /// 패밀리 타입 이름
        /// </summary>
        public string FamilyTypeName { get; set; } = string.Empty;

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
    }
}
