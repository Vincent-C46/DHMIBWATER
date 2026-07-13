using DHBIMWATER.Core.Settings;

namespace DHBIMWATER.Core.Quantity.RuleSets
{
    // 모든 프로젝트에 공통 적용되는 기본 수량 규칙 (콘크리트, 거푸집, 스페이서)
    //
    // Parameters 필터 키:
    //   MaterialClass : "콘크리트" | "강재" | "기타"  (Revit StructuralAssetClass)
    //   ConcWorkType  : "철근콘크리트" | "무근콘크리트"
    //   DH_IsExterior : "1" | "0"
    public static class DefaultRuleSet
    {
        // 기본 규칙 세트의 고정 ID: DataStorage 조회 시 항상 동일한 키로 식별하기 위해 하드코딩
        public static readonly Guid Id = Guid.Parse("00000000-0000-0000-0000-000000000001");

        // 거푸집 종류를 기본값으로 사용 (하위호환).
        public static RuleSet Create() => Create(new FormworkSettings());

        // 거푸집 종류를 설정창(FormworkSettings)에서 주입받아 규칙을 생성한다.
        public static RuleSet Create(FormworkSettings formwork) => new RuleSet
        {
            Id = Id,
            Name = "기본",
            Description = "콘크리트, 거푸집, 스페이서 공통 수량",
            Rules = BuildRules(formwork ?? new FormworkSettings()).ToList()
        };

        private static IEnumerable<QuantityRule> BuildRules(FormworkSettings fw)
        {
            var w  = fw.Walls;
            var c  = fw.Columns;
            var b  = fw.Beams;
            var f  = fw.Floors;
            var fo = fw.Foundation;

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
            // 오프닝 측면(A_opening_side_net)은 마구리(End) 설정을 따름 (설정창 힌트 참조)
            yield return Fw(w.Exterior, "A_right_net", Walls, [IsRc, IsExterior]);
            yield return Fw(w.Exterior, "A_left_net",  Walls, [IsRc, IsExterior]);
            yield return Fw(w.Interior, "A_right_net", Walls, [IsRc, IsInterior]);
            yield return Fw(w.Interior, "A_left_net", Walls, [IsRc, IsInterior]);
            yield return Fw(w.End, "A_end_net",          Walls, [IsRc]);
            yield return Fw(w.End, "A_opening_side_net", Walls, [IsRc]);

            // ── 거푸집: 기둥 ─────────────────────────────────────────────────
            yield return Fw(c.Side, "A_side_net", Columns, [IsRc]);

            // ── 거푸집: 보 ───────────────────────────────────────────────────
            // A_bottom_net(Framing) 제거: 보 하부면 거푸집은 슬래브 하부 거푸집에 포함
            yield return Fw(b.Side, "A_left_net",   Framing, [IsRc]);
            yield return Fw(b.Side, "A_right_net",  Framing, [IsRc]);
            yield return Fw(b.End,  "A_end_net",    Framing, [IsRc]);

            // ── 거푸집: 슬래브 ───────────────────────────────────────────────
            // 철근 (오프닝 측면은 옆면(SideRc) 설정을 따름)
            yield return Fw(f.Bottom, "A_bottom_net",       Floors, [IsRc]);
            yield return Fw(f.SideRc, "A_side_net",         Floors, [IsRc]);
            yield return Fw(f.SideRc, "A_opening_side_net", Floors, [IsRc]);
            // 무근
            yield return Fw(f.SidePlain, "A_side_net",         Floors, [IsPlain]);
            yield return Fw(f.SidePlain, "A_opening_side_net", Floors, [IsPlain]);

            // ── 거푸집: 기초 ─────────────────────────────────────────────────
            yield return Fw(fo.SideRc,    "A_side_net", Foundation, [IsRc]);
            yield return Fw(fo.SidePlain, "A_side_net", Foundation, [IsPlain]);

            // ── 스페이서: 벽체 ───────────────────────────────────────────────
            yield return Spacer("수직", "A_left_net",  Walls, [IsRc]);
            yield return Spacer("수직", "A_right_net", Walls, [IsRc]);

            // ── 스페이서: 슬래브 ─────────────────────────────────────────────
            yield return Spacer("수평", "A", Floors, [IsRc]);

            // ── 동바리: 강관 ─────────────────────────────────────────────────
            yield return SteelShoring("H≤3.5m",      "강관_3.5");
            yield return SteelShoring("3.5m<H≤4.2m", "강관_4.2");

            // ── 동바리: 시스템 ───────────────────────────────────────────────
            yield return SystemShoring("H≤5m",        "시스템_5");
            yield return SystemShoring("5m<H≤10m",    "시스템_10");
            yield return SystemShoring("10m<H≤20m",   "시스템_20");
            yield return SystemShoring("20m<H≤30m",   "시스템_30");

            // ── 철근 ─────────────────────────────────────────────────────────
            yield return new QuantityRule
            {
                WorkType = "철근",
                Specification = "",
                SpecParamName = "RebarKey",
                Formula = "L x N x UW x 0.001",
                Unit = "ton",
                CategoryIds = [Rebar],
                Filters = [],
            };
        }

        // ── 카테고리 ID 단축 ────────────────────────────────────────────────
        private static int Walls      => (int)RevitCategory.Walls;
        private static int Columns    => (int)RevitCategory.StructuralColumns;
        private static int Framing    => (int)RevitCategory.StructuralFraming;
        private static int Floors     => (int)RevitCategory.Floors;
        private static int Foundation => (int)RevitCategory.StructuralFoundation;
        private static int Generic    => (int)RevitCategory.GenericModel;
        private static int Rebar      => (int)RevitCategory.Rebar;

        // ── 공용 필터 ──────────────────────────────────────────────────────
        private static RuleFilter IsRc          => Filter("ConcWorkType",    "철근콘크리트");
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

        private static QuantityRule SteelShoring(string spec, string rangeKey) =>
            new()
            {
                WorkType      = "강관동바리",
                Specification = spec,
                Formula       = "A_bottom_net x ShoringFactor",
                Unit          = "m²",
                CategoryIds   = [Floors],
                Filters       = [Filter("ShoringRange", rangeKey)],
            };

        private static QuantityRule SystemShoring(string spec, string rangeKey) =>
            new()
            {
                WorkType      = "시스템동바리",
                Specification = spec,
                Formula       = "A_bottom_net x H_shoring x ShoringFactor",
                Unit          = "공m³",
                CategoryIds   = [Floors],
                Filters       = [Filter("ShoringRange", rangeKey)],
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
