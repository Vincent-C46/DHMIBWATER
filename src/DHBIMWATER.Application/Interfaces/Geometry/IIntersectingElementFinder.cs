using DHBIMWATER.Core.Quantity;

namespace DHBIMWATER.Application.Interfaces.Geometry
{
    public interface IIntersectingElementFinder
    {
        IReadOnlyList<FaceDeduction> FindContactAreas(long referenceElementId);
    }
}
