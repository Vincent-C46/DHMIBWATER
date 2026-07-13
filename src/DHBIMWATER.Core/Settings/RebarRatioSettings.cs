using DHBIMWATER.Core.Quantity;

namespace DHBIMWATER.Core.Settings
{
    /// <summary>
    /// 철근비(개략) 설정 — 카테고리별 kg/m³.
    /// 실물 배근(Rebar)과 무관하게 RC 콘크리트 체적 × 철근비로 "철근(개략)" 항목을 병행 산출한다.
    /// </summary>
    public class RebarRatioSettings
    {
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 카테고리별 철근비 (kg/m³). 기본값은 실무 개략치(가안).
        /// </summary>
        public Dictionary<RevitCategory, double> KgPerM3 { get; set; } = Defaults();

        private static Dictionary<RevitCategory, double> Defaults() => new()
        {
            [RevitCategory.StructuralFoundation] = 80,   // 기초
            [RevitCategory.Floors]               = 100,  // 슬래브
            [RevitCategory.Walls]                = 110,  // 벽
            [RevitCategory.StructuralFraming]    = 130,  // 보
            [RevitCategory.StructuralColumns]    = 150,  // 기둥
        };
    }
}
