using Autodesk.Revit.DB;
using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Infrastructure.Helpers;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Modeling;

public sealed class RevitPipeAlignmentCommandRepo : IPipeAlignmentCommandRepo
{
    private const double MinimumSegmentFeet = 1.0 / 256.0;
    private readonly Func<Document?> _doc;
    public RevitPipeAlignmentCommandRepo(Func<Document?> doc) => _doc = doc;

    public PipeAlignmentCreateResult Create(PipeAlignmentCreateDefinition definition)
    {
        var doc = _doc() ?? throw new InvalidOperationException("활성 Revit 문서를 찾을 수 없습니다.");
        var basePoint = AlignmentPlacementMapper.GetProjectBasePoint(doc);
        var created = 0; var skipped = 0; var warnings = new List<string>();
        foreach (var alignment in definition.Alignments)
        {
            if (alignment.Vertices.Count < 2) { warnings.Add($"{alignment.SourceFile} 레코드 {alignment.RecordNumber}: 정점이 2개 미만이라 건너뛰었습니다."); continue; }
            var lines = new List<GeometryObject>();
            for (var i = 1; i < alignment.Vertices.Count; i++)
            {
                var start = ToXyz(alignment.Vertices[i - 1], alignment.DiameterMm, definition, basePoint);
                var end = ToXyz(alignment.Vertices[i], alignment.DiameterMm, definition, basePoint);
                if (start.DistanceTo(end) < MinimumSegmentFeet) { skipped++; continue; }
                lines.Add(Line.CreateBound(start, end));
            }
            if (lines.Count == 0) { warnings.Add($"{alignment.SourceFile} 레코드 {alignment.RecordNumber}: 유효한 구간이 없어 건너뛰었습니다."); continue; }
            var shape = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
            shape.SetShape(lines);
            shape.Name = $"관로_{alignment.PipeKind}_{alignment.RecordNumber}";
            SetText(shape, "DH_Addin", "DHBIMWATER");
            SetText(shape, "DH_Category", "관로");
            SetText(shape, "DH_Part", alignment.PipeKind);
            SetLength(shape, "DH_구경", alignment.DiameterMm);
            SetLength(shape, "DH_연장", CalculateLengthMm(alignment.Vertices));
            SetNumber(shape, "DH_시점표고", alignment.Vertices[0].Z);
            SetNumber(shape, "DH_종점표고", alignment.Vertices[^1].Z);
            SetText(shape, "DH_원본파일", alignment.SourceFile);
            SetText(shape, "DH_레코드번호", alignment.RecordNumber);
            created++;
        }
        return new PipeAlignmentCreateResult(created, skipped, warnings);
    }

    private static XYZ ToXyz(DHBIMWATER.Core.Geometry.Point3D point, double diameterMm, PipeAlignmentCreateDefinition definition, XYZ basePoint)
        => AlignmentPlacementMapper.ToXyz(point, diameterMm, definition.ReferenceX, definition.ReferenceY, definition.ZDatum, basePoint);

    private static double CalculateLengthMm(IReadOnlyList<DHBIMWATER.Core.Geometry.Point3D> vertices) => vertices.Zip(vertices.Skip(1), (a, b) => a.DistanceTo(b)).Sum() * 1000;
    private static void SetText(Element element, string name, string value) => element.LookupParameter(name)?.Set(value ?? string.Empty);
    private static void SetLength(Element element, string name, double millimeters) => element.LookupParameter(name)?.Set(UC.MmToFt(millimeters));
    private static void SetNumber(Element element, string name, double value) => element.LookupParameter(name)?.Set(value);
}
