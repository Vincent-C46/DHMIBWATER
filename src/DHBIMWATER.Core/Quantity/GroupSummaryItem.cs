namespace DHBIMWATER.Core.Quantity
{
    public record GroupSummaryItem
    {
        public string Name         { get; init; } = string.Empty;
        public string Spec         { get; init; } = string.Empty;
        public string ValueDisplay { get; init; } = string.Empty;
        public string Unit         { get; init; } = string.Empty;
    }
}