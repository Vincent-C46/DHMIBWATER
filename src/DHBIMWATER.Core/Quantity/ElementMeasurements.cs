namespace DHBIMWATER.Core.Quantity
{
    // Values 표준 키 (카테고리별로 채워지는 키가 다름):
    //   공통      : Vol
    //   벽/보/기둥 : L, H, B, D, R(원형기둥), Thk(벽/슬래브), A(카테고리별 의미 상이)
    //   단면적     : A_cs (기둥/보 전용, cross-section)
    //   면 면적    : A_{facetype}_{net|gross}
    //               facetype = left | right | end | side | top | bottom
    //               예) A_left_net, A_bottom_gross
    //
    // Parameters 표준 키:
    //   MaterialClass   : "콘크리트" | "강재" | "기타"  (Revit StructuralAssetClass 직접 매핑)
    //   ConcWorkType    : "철근콘크리트" | "무근콘크리트"  (콘크리트 내 세분류, Extractor에서 판별)
    //   MaterialName    : Revit 재료명
    //   DH_IsExterior   : "1" | "0"
    //   DH_ElementCode  : "W1" 등
    //   IsCircular      : "true" | "false" (기둥 전용)
    public class ElementMeasurements
    {
        public long ElementId { get; set; }
        public string Category { get; set; } = string.Empty;   // 표시용
        public int CategoryId { get; set; }                    // BuiltInCategory int 값 (로케일 무관)
        public Dictionary<string, double> Values { get; set; } = new();
        public Dictionary<string, string> Parameters { get; set; } = new();
    }
}