namespace DHBIMWATER.Core.Quantity
{
    // 모든 프로젝트에 공통 적용되는 기본 수량 규칙 (콘크리트, 거푸집, 스페이서)
    // 프로젝트별 RuleSet은 이 규칙 위에 추가 적용됨
    //
    // Parameters 필터 키:
    //   MaterialClass : "콘크리트" | "강재" | "기타"  (Revit StructuralAssetClass)
    //   ConcWorkType  : "철근콘크리트" | "무근콘크리트"  (콘크리트 내 세분류)
    //   DH_IsExterior : "1" | "0"
    public static class DefaultRuleSet
    {
        public static readonly Guid Id = Guid.Parse("00000000-0000-0000-0000-000000000001");

        public static RuleSet Create() => new RuleSet
        {
            Id = Id,
            Name = "기본",
            Description = "콘크리트, 거푸집, 스페이서 공통 수량",
            Rules = BuildRules().ToList()
        };

        private static IEnumerable<QuantityRule> BuildRules()
        {
            // ── 철근콘크리트 ────────────────────────────────────────────────
            // Specification 빈 string → 엔진이 Parameters["MaterialName"]으로 채움
            yield return Rc("철근콘크리트", "", "Vol", "m³",
                categories: ["구조 벽체", "구조 기둥", "구조 프레이밍", "바닥", "구조 기초"],
                filters: [IsRc]);

            // ── 강재 ────────────────────────────────────────────────────────
            yield return new QuantityRule
            {
                WorkType = "강재",
                Specification = "",
                Formula = "A_cs x L x UW",
                Unit = "ton",
                ApplicableCategories = ["구조 기둥", "구조 프레이밍"],
                Filters = [IsSteel],
                Constants = new() { ["UW"] = 7.850 }
            };

            // ── 무근콘크리트 ─────────────────────────────────────────────────
            yield return Rc("무근콘크리트", "", "Vol", "m³",
                categories: ["바닥", "구조 기초"],
                filters: [IsPlain]);


            // ── 거푸집: 벽체 ─────────────────────────────────────────────────
            // 외벽 양면
            yield return Fw(FormworkType.Euroform, "A_right_net", "구조 벽체", [IsRc, IsExterior]);
            yield return Fw(FormworkType.Euroform, "A_left_net",  "구조 벽체", [IsRc, IsExterior]);
            // 내벽 양면
            yield return Fw(FormworkType.Euroform, "A_left_net",  "구조 벽체", [IsRc, IsInterior]);
            yield return Fw(FormworkType.Euroform, "A_right_net", "구조 벽체", [IsRc, IsInterior]);
            // 마구리
            yield return Fw(FormworkType.Plywood3, "A_end_net", "구조 벽체", [IsRc]);

            // ── 거푸집: 기둥 ─────────────────────────────────────────────────
            yield return Fw(FormworkType.Plywood3, "A_side_net", "구조 기둥", [IsRc]);

            // ── 거푸집: 보 ───────────────────────────────────────────────────
            yield return Fw(FormworkType.Plywood4, "A_bottom_net", "구조 프레이밍", [IsRc]);
            yield return Fw(FormworkType.Plywood3, "A_left_net",   "구조 프레이밍", [IsRc]);
            yield return Fw(FormworkType.Plywood3, "A_right_net",  "구조 프레이밍", [IsRc]);
            yield return Fw(FormworkType.Plywood3, "A_end_net",    "구조 프레이밍", [IsRc]);

            // ── 거푸집: 슬래브 ───────────────────────────────────────────────
            yield return Fw(FormworkType.Plywood4, "A_bottom_net", "바닥", [IsRc]);
            yield return Fw(FormworkType.Plywood3, "A_side_net",   "바닥", [IsRc]);
            yield return Fw(FormworkType.Plywood6, "A_side_net",   "바닥", [IsPlain]);

            // ── 거푸집: 기초 ─────────────────────────────────────────────────
            yield return Fw(FormworkType.Plywood4, "A_side_net", "구조 기초", [IsRc]);
            yield return Fw(FormworkType.Plywood6, "A_side_net", "구조 기초", [IsPlain]);

            // ── 스페이서: 벽체 ───────────────────────────────────────────────
            yield return Spacer("수직", "A_left_net",  "구조 벽체", [IsRc]);
            yield return Spacer("수직", "A_right_net", "구조 벽체", [IsRc]);

            // ── 스페이서: 슬래브 ─────────────────────────────────────────────
            yield return Spacer("수평", "A", "바닥", [IsRc]);
        }

        // ── 공용 필터 ──────────────────────────────────────────────────────
        private static RuleFilter IsRc       => Filter("ConcWorkType",  "철근콘크리트");
        private static RuleFilter IsPlain    => Filter("ConcWorkType",  "무근콘크리트");
        private static RuleFilter IsSteel    => Filter("MaterialClass", "강재");
        private static RuleFilter IsExterior => Filter("DH_IsExterior", "1");
        private static RuleFilter IsInterior => Filter("DH_IsExterior", "0");

        private static RuleFilter Filter(string param, string value) =>
            new() { ParameterName = param, Operator = FilterOperator.Equals, Value = value };

        // ── 팩토리 헬퍼 ───────────────────────────────────────────────────
        private static QuantityRule Rc(string workType, string spec, string formula, string unit,
            List<string> categories, List<RuleFilter> filters) =>
            new()
            {
                WorkType = workType,
                Specification = spec,
                Formula = formula,
                Unit = unit,
                ApplicableCategories = categories,
                Filters = filters
            };

        private static QuantityRule Fw(FormworkType type, string formula, string category,
            List<RuleFilter> filters) =>
            new()
            {
                WorkType = "거푸집",
                Specification = type.ToSpecification(),
                Formula = formula,
                Unit = "m²",
                ApplicableCategories = [category],
                Filters = filters
            };

        private static QuantityRule Spacer(string spec, string formula, string category,
            List<RuleFilter> filters) =>
            new()
            {
                WorkType = "스페이서",
                Specification = spec,
                Formula = formula,
                Unit = "m²",
                ApplicableCategories = [category],
                Filters = filters
            };
    }
}
