using DHBIMWATER.Application.DTOs.Gis;
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
    public int PlaceAlong(IReadOnlyList<PipeAlignment> alignments, string beamTypeName, string? levelName, double intervalM, bool alignTangent, AlignmentPlacementOrigin origin)
    {
        var doc = _doc() ?? throw new InvalidOperationException("활성 Revit 문서가 없습니다.");
        var type = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralFraming).WhereElementIsElementType().Cast<FamilySymbol>().FirstOrDefault(x => x.Name == beamTypeName) ?? throw new InvalidOperationException($"빔 유형을 찾을 수 없습니다: {beamTypeName}");
        var level = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().FirstOrDefault(x => levelName is null || x.Name == levelName) ?? throw new InvalidOperationException("레벨을 찾을 수 없습니다.");
        if (!type.IsActive) type.Activate();
        // Revit 짧은 커브 허용치(약 0.00256ft ≈ 0.78mm)보다 짧은 세그먼트는 보 생성이 불가하므로 건너뛴다.
        var minLengthFt = doc.Application.ShortCurveTolerance;
        var count = 0;
        // SamplePoints(점 배치)가 아니라 SampleSegments로 intervalM(6m)마다 끊어 시작/끝점을 잇는 선 기반 보를 생성한다.
        // alignTangent는 선 기반 보에서는 커브가 곧 방향이므로 사용하지 않는다(회전 불필요).
        foreach (var alignment in alignments) foreach (var segment in AlignmentIntervalSampler.SampleSegments(alignment.Vertices, intervalM))
        {
            var start = ToXyz(segment.Start, alignment.DiameterMm, origin);
            var end = ToXyz(segment.End, alignment.DiameterMm, origin);
            if (start.DistanceTo(end) < minLengthFt) continue;
            doc.Create.NewFamilyInstance(Line.CreateBound(start, end), type, level, StructuralType.Beam);
            count++;
        }
        return count;
    }
    private static XYZ ToXyz(DHBIMWATER.Core.Geometry.Point3D point, double diameterMm, AlignmentPlacementOrigin origin)
    {
        var z = origin.ZDatum switch
        {
            ZDatum.Invert => point.Z + diameterMm / 2000.0,
            ZDatum.Crown => point.Z - diameterMm / 2000.0,
            _ => point.Z
        };
        return new XYZ(UC.MmToFt((point.X - origin.X) * 1000), UC.MmToFt((point.Y - origin.Y) * 1000), UC.MmToFt((z - origin.Z) * 1000));
    }
}
