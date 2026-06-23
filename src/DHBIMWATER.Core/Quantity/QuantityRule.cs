namespace DHBIMWATER.Core.Quantity
{
    public class QuantityRule
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string WorkType { get; set; } = string.Empty;
        public string Specification { get; set; } = string.Empty;
        // Specification이 빈 string일 때 Parameters에서 읽을 키. 기본값은 "MaterialName"
        public string SpecParamName { get; set; } = "MaterialName";
        public string Formula { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;

        // 비어있으면 전체 카테고리에 적용. (int)BuiltInCategory 값으로 비교
        public List<int> CategoryIds { get; set; } = new();

        // 모든 조건 AND. 빈 리스트면 필터 없이 전체 적용
        public List<RuleFilter> Filters { get; set; } = new();

        // Formula 변수 중 ElementMeasurements.Values에 없는 사용자 정의 상수
        // 예) { "CJ": 2 }  →  "L x CJ" 에서 CJ = 2
        public Dictionary<string, double> Constants { get; set; } = new();
    }
}
