using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Geometry;

namespace DHBIMWATER.Application.UseCases
{
    public class ClassifyExteriorWallsUseCase
    {
        private readonly IExteriorWallClassifierRepo _repo;

        // 벽체 중심점이 hull 경계로부터 이 거리(mm) 이내면 외벽으로 판단
        private const double ToleranceMm = 500.0;

        public ClassifyExteriorWallsUseCase(IExteriorWallClassifierRepo repo)
        {
            _repo = repo;
        }

        public IReadOnlyList<Point2D> Execute()
        {
            var walls = _repo.GetWallMidpoints();
            if (walls.Count == 0) return [];

            var hull = ConvexHull.Compute(walls.Select(w => w.Midpoint));

            foreach (var (elementId, midpoint) in walls)
            {
                bool isExterior = ConvexHull.IsOnBoundary(midpoint, hull, ToleranceMm);
                _repo.SetExteriorFlag(elementId, isExterior);
            }

            return hull;
        }
    }
}
