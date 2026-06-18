using DHBIMWATER.Core.Geometry;

namespace DHBIMWATER.Application.Interfaces
{
    public interface IExteriorWallClassifierRepo
    {
        IReadOnlyList<(int ElementId, Point2D Midpoint)> GetWallMidpoints();
        void SetExteriorFlag(int elementId, bool isExterior);
    }
}
