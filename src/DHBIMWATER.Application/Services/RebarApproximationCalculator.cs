using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Core.Settings;

namespace DHBIMWATER.Application.Services
{
    /// <summary>
    /// 철근(개략) 병행 산출 — RC 콘크리트 항목의 체적 × 카테고리별 철근비(kg/m³) ÷ 1000 = [ton].
    /// 실물 배근(Rebar) 산출과 별개 공종이며, 합계에는 함께 더해지지 않는다(표시/집계 단계 처리).
    /// </summary>
    public static class RebarApproximationCalculator
    {
        private const string WorkTypeConcreteRc = "철근콘크리트";
        public const string WorkTypeRebarApprox = "철근(개략)";

        public static IEnumerable<QuantityItem> Create(
            IEnumerable<QuantityItem> concreteItems, RebarRatioSettings settings)
        {
            if (settings is null || !settings.Enabled)
                yield break;

            foreach (var item in concreteItems)
            {
                if (item.WorkType != WorkTypeConcreteRc)
                    continue;

                // 카테고리별 철근비 조회 (미등록·0 이하이면 산출 안 함)
                if (!settings.KgPerM3.TryGetValue((RevitCategory)item.CategoryId, out var kgPerM3) || kgPerM3 <= 0)
                    continue;

                var tons = item.Value * kgPerM3 / 1000.0;
                if (tons <= 1e-9)
                    continue;

                yield return new QuantityItem
                {
                    WorkType        = WorkTypeRebarApprox,
                    Unit            = "ton",
                    ElementId       = item.ElementId,
                    HostElementId   = item.HostElementId,
                    CategoryId      = item.CategoryId,
                    Category        = item.Category,
                    ElementCode     = item.ElementCode,
                    Value           = tons,
                    RawFormula      = "V × ρ ÷ 1000",
                    RenderedFormula = $"{item.Value:0.###}(V) × {kgPerM3:0.#}(ρ) ÷ 1000",
                    Status          = QuantityStatus.Auto,
                };
            }
        }
    }
}
