using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Core.Quantity
{
    public record QuantityItem
    {
        // 필수
        public required string WorkType { get; init; }      // 공종: 콘크리트, 거푸집
        public string Unit { get; init; } = string.Empty;          // 단위: m³, m², 무단위

        public long ElementId { get; init; }                       // ElementId
        public long? HostElementId { get; init; } = null;          // 호스트객체 ID: 철근·오프닝 등 종속 객체에 활용
        public string Category { get; init; } = string.Empty;      // 카테고리: 보, 벽, 헌치 등
        public string ElementCode { get; init; } = string.Empty;   // 코드: G1, W1 (중복가능)

        public string Specification { get; init; } = string.Empty; // 규격1: 유로폼, 25-30-250 ... 
        public string SubSpecification { get; init; } = string.Empty; // 규격2: 0~7m ...

        public string RawFormula { get; init; } = string.Empty;       // 산식: B × D × L
        public string RenderedFormula { get; init; } = string.Empty;   // 산식: 0.6(B) × 0.7(D) × 10.0(L)
        public double Value { get; init; }                         // 값: 5.0
    }
}