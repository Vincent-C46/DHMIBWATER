using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;

namespace DHBIMWATER.Infrastructure.Repositories.Mock.Quantity
{
    public class MockWallExtractor : IQuantityExtractor
    {
        public bool CanExtract(long elementId)
        {
            return false;
        }

        public IEnumerable<long> CollectElementIds()
        {
            return new List<long>();
        }

        public IEnumerable<QuantityItem> Extract(long elementId)
        {
            return new List<QuantityItem>
            {
                new QuantityItem
                {
                    ElementId = elementId,
                    HostElementId = null,
                    Category = "Walls",
                    ElementCode = "WALL-001",
                    WorkType = "Construction",
                    Specification = "Generic Wall",
                    RenderedFormula = "Length * Height",
                    Value = 100.0,
                    Unit = "m²"
                }
            };
        }
    }
}
