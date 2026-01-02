using System.Collections.Generic;

namespace DHBIMWATER.Application.DTOs.Revit.Elements
{
    /// <summary>
    /// Revit 요소의 기본 정보를 담는 DTO
    /// </summary>
    public class RevitElementDto
    {
        /// <summary>
        /// 요소 ID
        /// </summary>
        public int ElementId { get; set; }

        /// <summary>
        /// 요소 이름
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 카테고리 이름
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// 레벨 이름
        /// </summary>
        public string? LevelName { get; set; }

        /// <summary>
        /// 패밀리 이름
        /// </summary>
        public string? FamilyName { get; set; }

        /// <summary>
        /// 타입 이름
        /// </summary>
        public string? TypeName { get; set; }

        /// <summary>
        /// 파라미터 목록
        /// </summary>
        public List<RevitParameterDto> Parameters { get; set; } = new();
    }
}
