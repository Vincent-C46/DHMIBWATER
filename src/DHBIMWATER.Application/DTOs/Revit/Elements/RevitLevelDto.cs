namespace DHBIMWATER.Application.DTOs.Revit.Elements
{
    /// <summary>
    /// Revit 레벨 정보를 담는 DTO
    /// </summary>
    public class RevitLevelDto
    {
        /// <summary>
        /// 레벨 ID
        /// </summary>
        public int LevelId { get; set; }

        /// <summary>
        /// 레벨 이름
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 레벨 높이 (미터)
        /// </summary>
        public double Elevation { get; set; }

        /// <summary>
        /// 구조 레벨 여부
        /// </summary>
        public bool IsStructural { get; set; }
    }
}
