using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Core.Quantity
{
    public enum QuantityStatus
    {
        Auto,       // 자동 계산
        Modified,   // 자동 계산 후 수정
        Manual,     // 수동 추가
    }

    public record QuantityItem
    {
        // 필수
        public string WorkType { get; init; } = string.Empty;      // 공종: 콘크리트, 거푸집
        public string Unit { get; init; } = string.Empty;          // 단위: m³, m², 무단위. Enum 으로 변경 필요

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
        public List<FaceDeduction>? Deductions { get; init; } = null;

        public bool HasDeductions => Deductions != null && Deductions.Count > 0;

        public double? GrossValue { get; init; } = null;

        public string ElementGroupLabel
        {
            get
            {
                if (WorkType == "철근")
                    return string.IsNullOrEmpty(ElementCode) ? "(코드 없음)" : ElementCode;

                return string.IsNullOrEmpty(ElementCode)
                    ? $"(Id: {ElementId})"
                    : $"{ElementCode} (Id: {ElementId})";
            }
        }
    }
}
