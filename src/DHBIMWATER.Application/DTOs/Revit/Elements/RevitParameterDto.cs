namespace DHBIMWATER.Application.DTOs.Revit.Elements
{
    /// <summary>
    /// Revit 파라미터 정보를 담는 DTO
    /// </summary>
    public class RevitParameterDto
    {
        /// <summary>
        /// 파라미터 이름
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 파라미터 값 (문자열로 변환)
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// 파라미터 타입 (Length, Area, Volume, Text 등)
        /// </summary>
        public string ParameterType { get; set; } = string.Empty;

        /// <summary>
        /// 읽기 전용 여부
        /// </summary>
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// 저장 타입 (String, Double, Integer, ElementId 등)
        /// </summary>
        public string StorageType { get; set; } = string.Empty;
    }
}
