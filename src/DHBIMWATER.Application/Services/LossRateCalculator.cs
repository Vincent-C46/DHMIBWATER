using DHBIMWATER.Core.Settings;

namespace DHBIMWATER.Application.Services
{
    /// <summary>
    /// 할증률 계산 — 정미량 × (1 + 할증률/100).
    /// 정미량(QuantityItem.Value)은 유지하고, 집계·Excel 내보내기 단계에서만 할증 반영량을 병기하는 데 사용한다.
    /// </summary>
    public static class LossRateCalculator
    {
        /// <summary>
        /// 공종(workType)의 할증 반영량을 반환한다. 비활성이거나 등록된 할증률이 없으면 정미량 그대로.
        /// </summary>
        public static double Calculate(double netValue, string workType, LossRateSettings settings)
        {
            if (settings is null || !settings.Enabled)
                return netValue;

            var pct = settings.RatePercent.TryGetValue(workType, out var p) ? p : 0.0;
            return netValue * (1 + pct / 100.0);
        }
    }
}
