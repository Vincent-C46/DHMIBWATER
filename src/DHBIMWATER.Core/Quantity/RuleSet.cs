namespace DHBIMWATER.Core.Quantity
{
    public class RuleSet
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<QuantityRule> Rules { get; set; } = new();
    }
}
