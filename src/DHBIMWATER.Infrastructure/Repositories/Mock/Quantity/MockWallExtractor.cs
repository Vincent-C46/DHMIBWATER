using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity;

namespace DHBIMWATER.Infrastructure.Repositories.Mock.Quantity
{
    public class MockWallExtractor : IQuantityExtractor
    {
        public bool CanExtract(long elementId)
        {
            return true;
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
                    Material = "Concrete",
                    Formula = "Length * Height",
                    Value = 100.0,
                    Unit = "m²"
                }
            };
        }
    }
}
