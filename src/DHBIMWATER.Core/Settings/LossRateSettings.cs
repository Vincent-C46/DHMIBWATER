namespace DHBIMWATER.Core.Settings
{
    /// <summary>
    /// 할증률 설정 — 공종별 %.
    /// 정미량(QuantityItem.Value)은 유지하고, 집계표·Excel 내보내기 단계에서만
    /// 할증 반영량(정미량 × (1 + 할증률/100))을 별도 열로 병기한다.
    /// </summary>
    public class LossRateSettings
    {
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// key: WorkType, value: 할증률(%). 철근은 직경 구분 없이 일괄 3%.
        /// </summary>
        public Dictionary<string, double> RatePercent { get; set; } = Defaults();

        private static Dictionary<string, double> Defaults() => new()
        {
            ["철근콘크리트"] = 2,  // 레미콘
            ["무근콘크리트"] = 2,  // 레미콘
            ["철근"]       = 3,  // 이형철근, 직경 구분 없이 일괄
            ["거푸집"]      = 5,  // 합판
            ["강재"]       = 3,
        };
    }
}
