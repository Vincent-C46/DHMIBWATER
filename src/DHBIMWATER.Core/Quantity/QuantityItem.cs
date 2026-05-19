using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Core.Quantity
{
    public record QuantityItem
    {
        public long ElementId { get; init; }                       // ElementId
        public long? HostElementId { get; init; } = null;          // 호스트객체 ID: 철근·오프닝 등 종속 객체에 활용
        public string Category { get; init; } = string.Empty;      // 카테고리: 보, 벽, 헌치 등
        public string ElementCode { get; init; } = string.Empty;   // 코드: G1, W1 (중복가능)
        public string WorkType { get; init; } = string.Empty;      // 공종: 콘크리트, 거푸집
        public string Specification { get; init; } = string.Empty; // 규격: 300 x 500, 200 x 200, ..
        public string Material { get; init; } = string.Empty;      // 재료: 25-30-250, 유로폼, .. 
        public string Formula { get; init; } = string.Empty;       // 산식: B × D × L
        public double Value { get; init; }                         // 값: 5.0
        public string Unit { get; init; } = string.Empty;          // 단위: m³, m²
    }
}