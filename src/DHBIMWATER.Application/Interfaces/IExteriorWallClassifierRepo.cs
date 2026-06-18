using DHBIMWATER.Core.Geometry;

namespace DHBIMWATER.Application.Interfaces
{
    public interface IExteriorWallClassifierRepo
    {
        IReadOnlyList<(int ElementId, Point2D Start, Point2D End)> GetWallEndpoints();
        void SetExteriorFlag(int elementId, bool isExterior);
    }
}
