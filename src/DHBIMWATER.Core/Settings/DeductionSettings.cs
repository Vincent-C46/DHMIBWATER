using DHBIMWATER.Core.Quantity;

namespace DHBIMWATER.Core.Settings
{
    /// <summary>
    /// 면적 공제 설정 — 카테고리 간 접합부 공제 매트릭스 + 오프닝 최소 체적 임계값.
    /// 기본값은 현재 RevitIntersectingElementFinder 하드코딩 매핑과 동일.
    /// </summary>
    public class DeductionSettings
    {
        /// <summary>
        /// 호스트 카테고리별로 접촉면을 공제할 인접 카테고리 목록.
        /// </summary>
        public Dictionary<RevitCategory, List<RevitCategory>> CategoryMatrix { get; set; } = DefaultMatrix();

        /// <summary>
        /// 이 체적(m³) 미만 오프닝은 콘크리트·거푸집 공제에서 제외한다.
        /// </summary>
        public double OpeningMinVolumeM3 { get; set; } = 1.0;

        /// <summary>
        /// 오프닝 최소 체적 임계값 사용 여부.
        /// </summary>
        public bool UseOpeningMinVolume { get; set; } = true;

        private static Dictionary<RevitCategory, List<RevitCategory>> DefaultMatrix() => new()
        {
            // 벽: 벽·슬래브·기둥·보 (기초 제외)
            [RevitCategory.Walls] = new()
            {
                RevitCategory.Walls, RevitCategory.Floors,
                RevitCategory.StructuralColumns, RevitCategory.StructuralFraming
            },
            // 슬래브: 벽·슬래브·기초 (기둥·보 제외)
            [RevitCategory.Floors] = new()
            {
                RevitCategory.Walls, RevitCategory.Floors,
                RevitCategory.StructuralFoundation
            },
            // 기둥: 벽·슬래브·보·기초
            [RevitCategory.StructuralColumns] = new()
            {
                RevitCategory.Walls, RevitCategory.Floors,
                RevitCategory.StructuralFraming, RevitCategory.StructuralFoundation
            },
            // 보: 벽·슬래브·기둥·보 (기초 제외)
            [RevitCategory.StructuralFraming] = new()
            {
                RevitCategory.Walls, RevitCategory.Floors,
                RevitCategory.StructuralColumns, RevitCategory.StructuralFraming
            },
            // 계단: 벽·슬래브·기둥·보 (기초 제외)
            [RevitCategory.Stairs] = new()
            {
                RevitCategory.Walls, RevitCategory.Floors,
                RevitCategory.StructuralColumns, RevitCategory.StructuralFraming
            },
        };
    }
}
