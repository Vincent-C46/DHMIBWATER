namespace DHBIMWATER.Core.Quantity.RuleSets
{
    // 펌프장 전용 수량 규칙: DefaultRuleSet(공통) + 펌프장 특수 규칙
    // CJ(Constant Joint count): 기본값 1.0 → 추후 설정 Repository에서 주입 예정
    public static class PumpingStationRuleSet
    {
        // 기본 규칙 세트의 고정 ID: DataStorage 조회 시 항상 동일한 키로 식별하기 위해 하드코딩
        public static readonly Guid Id = Guid.Parse("00000000-0000-0000-0000-000000000002");

        public static RuleSet Create()
        {
            var ruleSet = DefaultRuleSet.Create();
            ruleSet.Id = Id;
            ruleSet.Name = "펌프장";
            ruleSet.Description = "공통 수량 + 펌프장 특수 수량(조인트 등)";
            ruleSet.Rules.AddRange(BuildRules());
            return ruleSet;
        }

        private static IEnumerable<QuantityRule> BuildRules()
        {
            // ── 지수판 ───────────────────────────────────────────────────────
            yield return new QuantityRule
            {
                WorkType = "지수판",
                Specification = "PVC, B=200",
                Formula = "L x CJ",
                Unit = "m",
                CategoryIds = [(int)RevitCategory.Walls],   // 벽체 카테고리 중에
                Filters =
                [
                    new RuleFilter
                    {
                        ParameterName = "ConcWorkType",
                        Operator = FilterOperator.Equals,
                        Value = "철근콘크리트"
                    }
                ],
                // TODO: CJ는 설정 Repository에서 주입받도록 변경 예정
                Constants = new() { ["CJ"] = 1.0 }
            };

            // ── 방수 ───────────────────────────────────────────────────────
            yield return new QuantityRule
            {
                WorkType = "방수",
                Specification = "수성페인트칠(롤러2회)",
                Formula = "L x CJ",
                Unit = "m",
                CategoryIds = [(int)RevitCategory.Walls],   // 벽체 카테고리 중에
                Filters =
                [
                    new RuleFilter
                    {
                        ParameterName = "ConcWorkType",
                        Operator = FilterOperator.Equals,
                        Value = "철근콘크리트"
                    }
                ],
                Constants = new() { ["CJ"] = 1.0 }
            };
        }
    }
}
