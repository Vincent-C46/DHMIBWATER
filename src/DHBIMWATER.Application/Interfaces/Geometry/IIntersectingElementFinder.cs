using DHBIMWATER.Core.Quantity;

namespace DHBIMWATER.Application.Interfaces.Geometry
{
    public interface IIntersectingElementFinder
    {
        IReadOnlyList<(FaceType FaceType, long NeighborId, double Area)> FindContactAreas(long referenceElementId);
    }
}
