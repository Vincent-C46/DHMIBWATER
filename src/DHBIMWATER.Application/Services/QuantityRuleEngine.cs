using DHBIMWATER.Core.Quantity;

namespace DHBIMWATER.Application.Services
{
    public class QuantityRuleEngine
    {
        public IEnumerable<QuantityItem> Apply(ElementMeasurements measurements, IEnumerable<QuantityRule> rules)
        {
            var applicable = rules.Where(r => r.IsEnabled &&
                (r.CategoryIds.Count == 0 || r.CategoryIds.Contains(measurements.CategoryId)));

            foreach (var rule in applicable)
            {
                if (!PassesFilters(measurements, rule.Filters))
                    continue;

                var varDict = new Dictionary<string, double>(measurements.Values);
                foreach (var (k, v) in rule.Constants)
                    varDict[k] = v;

                double value;
                try { value = FormulaCalculator.Calculate(rule.Formula, varDict); }
                catch { continue; }

                if (value <= 1e-6) continue;

                var spec = string.IsNullOrEmpty(rule.Specification)
                    ? measurements.Parameters.GetValueOrDefault(rule.SpecParamName, "")
                    : rule.Specification;

                yield return new QuantityItem
                {
                    ElementId      = measurements.ElementId,
                    HostElementId  = measurements.HostElementId,
                    CategoryId     = measurements.CategoryId,
                    Category       = measurements.Category,
                    ElementCode    = measurements.Parameters.GetValueOrDefault("DH_ElementCode", ""),
                    WorkType       = rule.WorkType,
                    Specification  = spec,
                    RawFormula     = rule.Formula,
                    RenderedFormula = FormulaCalculator.Render(rule.Formula, varDict),
                    Value = value,
                    Unit  = rule.Unit,
                };
            }
        }

        private static bool PassesFilters(ElementMeasurements m, IEnumerable<RuleFilter> filters)
        {
            foreach (var f in filters)
            {
                if (!m.Parameters.TryGetValue(f.ParameterName, out var actual))
                    return false;

                bool passes;
                switch (f.Operator)
                {
                    case FilterOperator.Equals:
                        passes = actual == f.Value;
                        break;
                    case FilterOperator.NotEquals:
                        passes = actual != f.Value;
                        break;
                    case FilterOperator.Contains:
                        passes = actual.Contains(f.Value);
                        break;
                    default:
                        if (double.TryParse(actual, out var a) && double.TryParse(f.Value, out var b))
                            passes = f.Operator switch
                            {
                                FilterOperator.GreaterThan        => a > b,
                                FilterOperator.LessThan           => a < b,
                                FilterOperator.GreaterThanOrEqual => a >= b,
                                FilterOperator.LessThanOrEqual    => a <= b,
                                _ => false
                            };
                        else
                            passes = false;
                        break;
                }

                if (!passes) return false;
            }
            return true;
        }
    }
}
