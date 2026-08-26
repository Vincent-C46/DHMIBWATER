using Autodesk.Revit.DB;
using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Core.Geometry;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Modeling;

public sealed class RevitPipeAlignmentQueryRepo : IPipeAlignmentQueryRepo
{
    private readonly Func<Document?> _doc;
    public RevitPipeAlignmentQueryRepo(Func<Document?> doc) => _doc = doc;

    public IReadOnlyList<PipeAlignmentQueryResult> GetByElementIds(IReadOnlyList<int> elementIds)
    {
        var doc = _doc() ?? throw new InvalidOperationException("활성 Revit 문서를 찾을 수 없습니다.");
        var results = new List<PipeAlignmentQueryResult>();
        foreach (var id in elementIds)
        {
            if (doc.GetElement(new ElementId(id)) is not DirectShape shape) continue;
            var lines = shape.get_Geometry(new Options())?.OfType<Line>().ToList();
            if (lines is null || lines.Count == 0) continue;

            var vertices = new List<Point3D> { ToPoint3D(lines[0].GetEndPoint(0)) };
            vertices.AddRange(lines.Select(line => ToPoint3D(line.GetEndPoint(1))));

            var kind = shape.LookupParameter("DH_Part")?.AsString() ?? string.Empty;
            var diameterFt = shape.LookupParameter("DH_구경")?.AsDouble() ?? 0;
            results.Add(new PipeAlignmentQueryResult((int)shape.Id.Value, vertices, kind, UC.FtToMm(diameterFt)));
        }
        return results;
    }

    private static Point3D ToPoint3D(XYZ p) => new(UC.FtToMm(p.X), UC.FtToMm(p.Y), UC.FtToMm(p.Z));
}
