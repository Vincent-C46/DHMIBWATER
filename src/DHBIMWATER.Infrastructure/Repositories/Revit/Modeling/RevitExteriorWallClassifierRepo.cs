using Autodesk.Revit.DB;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Geometry;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Modeling
{
    internal class RevitExteriorWallClassifierRepo : IExteriorWallClassifierRepo
    {
        private readonly Func<Document?> _doc;

        public RevitExteriorWallClassifierRepo(Func<Document?> doc)
        {
            _doc = doc;
        }

        public IReadOnlyList<(int ElementId, Point2D Start, Point2D End)> GetWallEndpoints()
        {
            var doc = _doc();
            if (doc == null) return [];

            return new FilteredElementCollector(doc)
                .OfClass(typeof(Wall))
                .WhereElementIsNotElementType()
                .Cast<Wall>()
                .Select(wall =>
                {
                    var curve = (wall.Location as LocationCurve)?.Curve;
                    if (curve == null) return ((int ElementId, Point2D Start, Point2D End)?)null;
                    var s = curve.GetEndPoint(0);
                    var e = curve.GetEndPoint(1);
                    return ((int)wall.Id.Value,
                            new Point2D(UC.FtToMm(s.X), UC.FtToMm(s.Y)),
                            new Point2D(UC.FtToMm(e.X), UC.FtToMm(e.Y)));
                })
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToList();
        }

        public void SetExteriorFlag(int elementId, bool isExterior)
        {
            var doc = _doc();
            if (doc == null) return;

            var wall = doc.GetElement(new ElementId((long)elementId)) as Wall;
            wall?.LookupParameter("DH_IsExterior")?.Set(isExterior ? 1 : 0);
        }
    }
}
