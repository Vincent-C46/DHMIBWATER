using ACadSharp.Entities;
using ACadSharp.IO;
using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Gis;
using System.IO;

namespace DHBIMWATER.Infrastructure.Repositories.Gis;

/// <summary>ACadSharp로 DWG ModelSpace의 관로 폴리선을 읽는다. 블록 참조 내부 엔티티는 읽지 않는다.</summary>
public sealed class DwgAlignmentReader : IAlignmentSourceReader
{
    public bool CanRead(string filePath) => string.Equals(Path.GetExtension(filePath), ".dwg", StringComparison.OrdinalIgnoreCase);

    public ShapefileReadResult Read(string filePath)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException("DWG 파일을 찾을 수 없습니다.", filePath);

        var document = DwgReader.Read(filePath);
        var features = document.ModelSpace.Entities.SelectMany((entity, index) => ToFeature(entity, filePath, index + 1)).ToList();
        var vertices = features.SelectMany(x => x.Vertices).ToList();
        // LAYER는 CAD 레이어명을 레이어 필터용으로 Attributes에 합쳐둔 것일 뿐 실제 XDATA 필드가 아니므로,
        // 직경·관종 필드 콤보박스에 노출되는 fields 목록에서는 제외한다.
        var fields = features.SelectMany(x => x.Attributes.Keys).Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(x => !string.Equals(x, "LAYER", StringComparison.OrdinalIgnoreCase))
            .Select(x => new ShapefileFieldInfo(x, 'C', 0, 0)).ToList();
        var warnings = features.Count == 0
            ? new List<string> { "ModelSpace에서 POLYLINE, POLYLINE3D 또는 LWPOLYLINE 엔티티를 찾지 못했습니다." }
            : new List<string>();

        return new ShapefileReadResult(features, fields, Extent(vertices), null, null, "DWG", features.Count,
            vertices.Count, features.Sum(x => Math.Max(0, x.Vertices.Count - 1)), warnings, features.FirstOrDefault()?.Attributes);
    }

    private static IEnumerable<PipeAlignment> ToFeature(Entity entity, string filePath, int index)
    {
        var vertices = entity switch
        {
            Polyline3D polyline => polyline.Vertices.Select(x => new Point3D(x.Location.X, x.Location.Y, x.Location.Z)).ToList(),
            Polyline2D polyline => polyline.Vertices.Select(x => new Point3D(x.Location.X, x.Location.Y, polyline.Elevation)).ToList(),
            LwPolyline polyline => polyline.Vertices.Select(x => new Point3D(x.Location.X, x.Location.Y, polyline.Elevation)).ToList(),
            _ => null
        };
        if (vertices is null || vertices.Count < 2) yield break;

        var attributes = new Dictionary<string, string>(CadXDataAttributeParser.Parse(
            entity.ExtendedData.SelectMany(x => x.Value.Records).Select(x => x.RawValue).OfType<string>()), StringComparer.OrdinalIgnoreCase)
        {
            ["LAYER"] = entity.Layer.Name
        };
        yield return new PipeAlignment(vertices, string.Empty, 0, Path.GetFileName(filePath), index.ToString(), attributes);
    }

    private static ShapefileExtent Extent(IReadOnlyList<Point3D> vertices) => vertices.Count == 0 ? ShapefileExtent.Empty
        : new ShapefileExtent(vertices.Min(x => x.X), vertices.Min(x => x.Y), vertices.Max(x => x.X), vertices.Max(x => x.Y), vertices.Min(x => x.Z), vertices.Max(x => x.Z));
}
