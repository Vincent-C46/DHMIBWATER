namespace DHBIMWATER.Core.Structures
{
    public record ConcreteSpec(int MaxAggregateSize, int CompressiveStrength, int Slump)
    {
        public string MaterialName => $"{MaxAggregateSize}-{CompressiveStrength}-{Slump}";
    }
}
