using DHBIMWATER.Core.Geometry;

namespace DHBIMWATER.Application.Interfaces
{
    public interface IExteriorWallClassifierRepo
    {
        IReadOnlyList<(long ElementId, Point2D Start, Point2D End)> GetWallEndpoints();
        void SetExteriorFlag(long elementId, bool isExterior);
    }
}
