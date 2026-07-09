using DHBIMWATER.Core.Quantity;

namespace DHBIMWATER.Application.Interfaces.Quantity
{
    public interface IElementMeasurementExtractor
    {
        bool CanExtract(long elementId);
        IEnumerable<long> CollectElementIds();
        ElementMeasurements Extract(long elementId);
    }
}
