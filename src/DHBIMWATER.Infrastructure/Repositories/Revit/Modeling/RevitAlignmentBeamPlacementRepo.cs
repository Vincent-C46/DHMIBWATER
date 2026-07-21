using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Core.Gis;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Modeling;

internal sealed class RevitAlignmentBeamPlacementRepo : IAlignmentBeamPlacementRepo
{
    private readonly Func<Document?> _doc;
    public RevitAlignmentBeamPlacementRepo(Func<Document?> doc) => _doc = doc;
    public int PlaceAlong(IReadOnlyList<PipeAlignment> alignments, string beamTypeName, string? levelName, double intervalM, bool alignTangent)
    {
        var doc = _doc() ?? throw new InvalidOperationException("활성 Revit 문서가 없습니다.");
        var type = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralFraming).WhereElementIsElementType().Cast<FamilySymbol>().FirstOrDefault(x => x.Name == beamTypeName) ?? throw new InvalidOperationException($"빔 유형을 찾을 수 없습니다: {beamTypeName}");
        var level = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().FirstOrDefault(x => levelName is null || x.Name == levelName) ?? throw new InvalidOperationException("레벨을 찾을 수 없습니다.");
        if (!type.IsActive) type.Activate();
        var reference = alignments.SelectMany(x => x.Vertices).FirstOrDefault();
        if (reference is null) return 0;
        var count = 0;
        foreach (var alignment in alignments) foreach (var sample in AlignmentIntervalSampler.SamplePoints(alignment.Vertices, intervalM))
        {
            var point = ToXyz(sample.Position, reference);
            var instance = doc.Create.NewFamilyInstance(point, type, level, StructuralType.NonStructural);
            if (alignTangent && Math.Abs(sample.Tangent.X) + Math.Abs(sample.Tangent.Y) > 1e-9)
                ElementTransformUtils.RotateElement(doc, instance.Id, Line.CreateBound(point, point + XYZ.BasisZ), Math.Atan2(sample.Tangent.Y, sample.Tangent.X));
            count++;
        }
        return count;
    }
    // PipingView와 동일하게 첫 유효 정점을 내부원점 기준점으로 사용한다 (GIS 절대좌표 방지).
    private static XYZ ToXyz(DHBIMWATER.Core.Geometry.Point3D point, DHBIMWATER.Core.Geometry.Point3D reference) => new(
        UC.MmToFt((point.X - reference.X) * 1000),
        UC.MmToFt((point.Y - reference.Y) * 1000),
        UC.MmToFt((point.Z - reference.Z) * 1000));
}
