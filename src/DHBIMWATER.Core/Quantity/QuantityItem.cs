using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Core.Quantity
{
    public enum QuantityStatus
    {
        Auto,
        Manual,
    }

    public record QuantityItem
    {
        // 필수
        public string WorkType { get; init; } = string.Empty;      // 공종: 콘크리트, 거푸집
        public string Unit { get; init; } = string.Empty;          // 단위: m³, m², 무단위

        public long ElementId { get; init; }                       // ElementId
        public long? HostElementId { get; init; } = null;          // 호스트객체 ID: 철근·오프닝 등 종속 객체에 활용
        public string Category { get; init; } = string.Empty;      // 카테고리: 보, 벽, 헌치 등
        public string ElementCode { get; init; } = string.Empty;   // 코드: G1, W1 (중복가능)

        public string Specification { get; init; } = string.Empty; // 규격1: 유로폼, 25-30-250 ... 
        public string SubSpecification { get; init; } = string.Empty; // 규격2: 0~7m ...

        public string RawFormula { get; init; } = string.Empty;       // 산식: B × D × L
        public string RenderedFormula { get; init; } = string.Empty;   // 산식: 0.6(B) × 0.7(D) × 10.0(L)
        public double Value { get; init; }                         // 값: 5.0 (최종 수량)
        public QuantityStatus Status { get; init; }

        // 공제 관련 (거푸집 등 면적 공제가 필요한 경우만 사용)
        // 콘크리트 등 공제가 불필요하면 null 또는 빈 리스트
        public List<DeductionDetail>? Deductions { get; init; } = null;
        
        // 공제가 있는지 여부
        public bool HasDeductions => Deductions != null && Deductions.Count > 0;
        
        // 총 수량 (공제 전) - 공제가 있을 때만 의미있음
        public double? GrossValue { get; init; } = null;
    }

    /// <summary>
    /// 공제 상세 정보 (거푸집 면적 공제 등)
    /// 예: 벽체 거푸집에서 개구부(Opening) 면적 공제
    /// </summary>
    public record DeductionDetail
    {
        public long ElementId { get; init; }              // 공제 객체 ElementId (추적/역추적용)
        public string ElementCode { get; init; } = "";    // 공제 객체 코드 (O1, O2...)
        public string Category { get; init; } = "";       // 카테고리 (개구부, 슬래브 등)
        public double Value { get; init; }                // 공제 수량
        public string Description { get; init; } = "";    // 설명 (선택)
        public string Formula { get; init; } = "";        // 공제 산출식 (선택, 예: "1.0 × 2.0")
    }
}