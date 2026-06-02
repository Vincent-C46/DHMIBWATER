using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Core.Quantity
{
    public record QuantitySummaryItem
    {
        public string WorkType { get; init; } = string.Empty;
        public string Specification { get; init; } = string.Empty;
        public string SubSpecification { get; init; } = string.Empty;
        public string Unit { get; init; } = string.Empty;
        public double Value { get; init; } // 규격별 소계 or 공종별 합계 -> IsTotal로 구분
        public bool IsTotal { get; init; } = false;
    }
}
