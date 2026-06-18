using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Geometry;

namespace DHBIMWATER.Application.UseCases
{
    public class ClassifyExteriorWallsUseCase
    {
        private readonly IExteriorWallClassifierRepo _repo;
        
        // 벽체 끝점이 hull 경계로부터 이 거리(mm) 이내면 외벽으로 판단
        private const double ToleranceMm = 500.0;

        public ClassifyExteriorWallsUseCase(IExteriorWallClassifierRepo repo)
        {
            _repo = repo;
        }

        public IReadOnlyList<Point2D> Execute()
        {
            var walls = _repo.GetWallEndpoints();
            if (walls.Count == 0) return [];

            var allPoints = walls.SelectMany(w => new[] { w.Start, w.End });
            var hull = ConvexHull.Compute(allPoints);

            foreach (var (elementId, start, end) in walls)
            {
                bool isExterior = ConvexHull.IsOnBoundary(start, hull, ToleranceMm)
                               || ConvexHull.IsOnBoundary(end, hull, ToleranceMm);
                _repo.SetExteriorFlag(elementId, isExterior);
            }
            return hull;
        }
    }
}
