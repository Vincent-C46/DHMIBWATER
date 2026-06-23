namespace DHBIMWATER.Core.Quantity
{
    // 모든 프로젝트에 공통 적용되는 기본 수량 규칙 (콘크리트, 거푸집, 스페이서)
    //
    // Parameters 필터 키:
    //   MaterialClass : "콘크리트" | "강재" | "기타"  (Revit StructuralAssetClass)
    //   ConcWorkType  : "철근콘크리트" | "무근콘크리트"
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
            yield return Rc("철근콘크리트", "", "A x Thk", "m³", [Walls], [IsRc]);

            // 시스템 패밀리: Floor는 A x Thk, Foundation(독립기초 FamilyInstance)은 Vol
            yield return Rc("철근콘크리트", "", "A x Thk", "m³", [Floors],     [IsRc]);
            yield return Rc("철근콘크리트", "", "Vol",      "m³", [Foundation], [IsRc]);

            // FamilyInstance (Column, Beam): 단면 판별 후 공식 분기
            yield return Rc("철근콘크리트", "", "B x D x L",    "m³", [Columns, Framing], [IsRc, IsRectFormula]);
            yield return Rc("철근콘크리트", "", "PI x R^2 x L", "m³", [Columns],          [IsRc, IsCircular]);
            yield return Rc("철근콘크리트", "", "A_cs x L",     "m³", [Columns, Framing],
                [IsRc, Filter("UseRectFormula", "false"), Filter("IsCircular", "false")]);

            // ── 강재 ────────────────────────────────────────────────────────
            yield return new QuantityRule
            {
                WorkType = "강재",
                Specification = "",
                Formula = "A_cs x L x UW",
                Unit = "ton",
                CategoryIds = [Columns, Framing],
                Filters = [IsSteel],
                Constants = new() { ["UW"] = 7.850 }
            };

            // ── 무근콘크리트 ─────────────────────────────────────────────────
            yield return Rc("무근콘크리트", "", "A x Thk", "m³", [Floors],     [IsPlain]);
            yield return Rc("무근콘크리트", "", "Vol",      "m³", [Foundation], [IsPlain]);

            // ── 거푸집: 벽체 ─────────────────────────────────────────────────
            yield return Fw(FormworkType.Euroform, "A_right_net", Walls, [IsRc, IsExterior]);
            yield return Fw(FormworkType.Euroform, "A_left_net",  Walls, [IsRc, IsExterior]);
            yield return Fw(FormworkType.Euroform, "A_left_net",  Walls, [IsRc, IsInterior]);
            yield return Fw(FormworkType.Euroform, "A_right_net", Walls, [IsRc, IsInterior]);
            yield return Fw(FormworkType.Plywood3, "A_end_net",   Walls, [IsRc]);

            // ── 거푸집: 기둥 ─────────────────────────────────────────────────
            yield return Fw(FormworkType.Plywood3, "A_side_net", Columns, [IsRc]);

            // ── 거푸집: 보 ───────────────────────────────────────────────────
            yield return Fw(FormworkType.Plywood4, "A_bottom_net", Framing, [IsRc]);
            yield return Fw(FormworkType.Plywood3, "A_left_net",   Framing, [IsRc]);
            yield return Fw(FormworkType.Plywood3, "A_right_net",  Framing, [IsRc]);
            yield return Fw(FormworkType.Plywood3, "A_end_net",    Framing, [IsRc]);

            // ── 거푸집: 슬래브 ───────────────────────────────────────────────
            yield return Fw(FormworkType.Plywood4, "A_bottom_net", Floors, [IsRc]);
            yield return Fw(FormworkType.Plywood3, "A_side_net",   Floors, [IsRc]);
            yield return Fw(FormworkType.Plywood6, "A_side_net",   Floors, [IsPlain]);

            // ── 거푸집: 기초 ─────────────────────────────────────────────────
            yield return Fw(FormworkType.Plywood4, "A_side_net", Foundation, [IsRc]);
            yield return Fw(FormworkType.Plywood6, "A_side_net", Foundation, [IsPlain]);

            // ── 스페이서: 벽체 ───────────────────────────────────────────────
            yield return Spacer("수직", "A_left_net",  Walls, [IsRc]);
            yield return Spacer("수직", "A_right_net", Walls, [IsRc]);

            // ── 스페이서: 슬래브 ─────────────────────────────────────────────
            yield return Spacer("수평", "A", Floors, [IsRc]);
        }

        // ── 카테고리 ID 단축 ────────────────────────────────────────────────
        private static int Walls      => (int)RevitCategory.Walls;
        private static int Columns    => (int)RevitCategory.StructuralColumns;
        private static int Framing    => (int)RevitCategory.StructuralFraming;
        private static int Floors     => (int)RevitCategory.Floors;
        private static int Foundation => (int)RevitCategory.StructuralFoundation;
        private static int Generic => (int)RevitCategory.GenericModel;

        // ── 공용 필터 ──────────────────────────────────────────────────────
        private static RuleFilter IsRc          => Filter   ("ConcWorkType",    "철근콘크리트");
        private static RuleFilter IsPlain       => Filter("ConcWorkType",    "무근콘크리트");
        private static RuleFilter IsSteel       => Filter("MaterialClass",   "강재");
        private static RuleFilter IsExterior    => Filter("DH_IsExterior",   "1");
        private static RuleFilter IsInterior    => Filter("DH_IsExterior",   "0");
        private static RuleFilter IsRectFormula => Filter("UseRectFormula",  "true");
        private static RuleFilter IsCircular    => Filter("IsCircular",      "true");

        private static RuleFilter Filter(string param, string value) =>
            new() { ParameterName = param, Operator = FilterOperator.Equals, Value = value };

        // ── 팩토리 헬퍼 ───────────────────────────────────────────────────
        private static QuantityRule Rc(string workType, string spec, string formula, string unit,
            int[] categories, List<RuleFilter> filters) =>
            new()
            {
                WorkType = workType,
                Specification = spec,
                Formula = formula,
                Unit = unit,
                CategoryIds = [.. categories],
                Filters = filters
            };

        private static QuantityRule Fw(FormworkType type, string formula, int category,
            List<RuleFilter> filters) =>
            new()
            {
                WorkType = "거푸집",
                Specification = type.ToSpecification(),
                Formula = formula,
                Unit = "m²",
                CategoryIds = [category],
                Filters = filters
            };

        private static QuantityRule Spacer(string spec, string formula, int category,
            List<RuleFilter> filters) =>
            new()
            {
                WorkType = "스페이서",
                Specification = spec,
                Formula = formula,
                Unit = "m²",
                CategoryIds = [category],
                Filters = filters
            };
    }
}
