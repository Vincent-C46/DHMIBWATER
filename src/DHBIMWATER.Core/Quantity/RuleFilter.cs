namespace DHBIMWATER.Core.Quantity
{
    public enum FilterOperator
    {
        Equals,
        NotEquals,
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual,
        Contains,
    }

    public class RuleFilter
    {
        public string ParameterName { get; set; } = string.Empty;
        public FilterOperator Operator { get; set; }
        public string Value { get; set; } = string.Empty;
    }
}
