using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Gis;
using System.IO;

namespace DHBIMWATER.Infrastructure.Repositories.Gis;

/// <summary>ASCII DXF의 ENTITIES 폴리라인을 관로 선형으로 읽는다.</summary>
public sealed class DxfAlignmentReader : IAlignmentSourceReader
{
    public bool CanRead(string filePath) => string.Equals(Path.GetExtension(filePath), ".dxf", StringComparison.OrdinalIgnoreCase);
    public ShapefileReadResult Read(string filePath)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException("DXF 파일을 찾을 수 없습니다.", filePath);
        var entities = DxfGeometryReader.Read(filePath);
        var features = entities.Select((x, i) => new PipeAlignment(x.Vertices, string.Empty, 0, Path.GetFileName(filePath), (i + 1).ToString(), x.Attributes)).ToList();
        var warnings = features.Count == 0 ? new List<string> { "POLYLINE 또는 LWPOLYLINE 엔티티를 찾지 못했습니다. DWG 파일이 아닌 ASCII DXF인지 확인하세요." } : new List<string>();
        var extent = features.SelectMany(x => x.Vertices).ToList();
        var fields = features.SelectMany(x => x.Attributes.Keys).Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(x => !string.Equals(x, "LAYER", StringComparison.OrdinalIgnoreCase))
            .Select(x => new ShapefileFieldInfo(x, 'C', 0, 0)).ToList();
        return new ShapefileReadResult(features, fields, extent.Count == 0 ? ShapefileExtent.Empty : new ShapefileExtent(extent.Min(x => x.X), extent.Min(x => x.Y), extent.Max(x => x.X), extent.Max(x => x.Y), extent.Min(x => x.Z), extent.Max(x => x.Z)), null, null, "UTF-8", features.Count, extent.Count, features.Sum(x => Math.Max(0, x.Vertices.Count - 1)), warnings, features.FirstOrDefault()?.Attributes);
    }
}

internal static class DxfGeometryReader
{
    internal sealed record Entity(IReadOnlyList<Point3D> Vertices, IReadOnlyDictionary<string, string> Attributes);
    public static IReadOnlyList<Entity> Read(string path)
    {
        var pairs = File.ReadAllLines(path).Chunk(2).Where(x => x.Length == 2).Select(x => (Code: x[0].Trim(), Value: x[1].Trim())).ToList();
        var entities = new List<Entity>(); var inEntities = false;
        for (var i = 0; i < pairs.Count; i++)
        {
            if (pairs[i] == ("0", "SECTION") && i + 1 < pairs.Count && pairs[i + 1] == ("2", "ENTITIES")) { inEntities = true; i++; continue; }
            if (inEntities && pairs[i] == ("0", "ENDSEC")) break;
            if (!inEntities || pairs[i].Code != "0" || (pairs[i].Value != "LWPOLYLINE" && pairs[i].Value != "POLYLINE")) continue;
            var type = pairs[i].Value; var end = i + 1;
            while (end < pairs.Count && pairs[end].Code != "0") end++;
            if (type == "POLYLINE") while (end < pairs.Count && pairs[end] == ("0", "VERTEX")) { end++; while (end < pairs.Count && pairs[end].Code != "0") end++; }
            var block = pairs.Skip(i).Take(end - i).ToList();
            var vertices = type == "LWPOLYLINE" ? ReadLwVertices(block) : ReadVertices(block);
            if (vertices.Count >= 2) entities.Add(new Entity(vertices, ReadAttributes(block)));
            i = end - 1;
        }
        return entities;
    }
    private static List<Point3D> ReadLwVertices(IReadOnlyList<(string Code, string Value)> pairs)
    {
        // LWPOLYLINE은 평면 폴리선이라 정점별 Z(30)를 갖지 않고 엔티티 단위 표고(38)를 사용한다.
        // 38은 정점(10/20)보다 앞에 오는 것이 표준이지만, 순서에 의존하지 않도록 먼저 스캔한다.
        var elevation = pairs.Where(p => p.Code == "38").Select(p => Number(p.Value)).DefaultIfEmpty(0d).First();
        var result = new List<Point3D>(); double? x = null, y = null; var z = elevation;
        foreach (var pair in pairs)
        {
            if (pair.Code == "10") { if (x.HasValue && y.HasValue) result.Add(new Point3D(x.Value, y.Value, z)); x = Number(pair.Value); y = null; }
            // 30은 표준 LWPOLYLINE에는 없지만 일부 변환기가 내보내므로 있으면 표고보다 우선 적용
            else if (pair.Code == "20") y = Number(pair.Value); else if (pair.Code == "30") z = Number(pair.Value);
        }
        if (x.HasValue && y.HasValue) result.Add(new Point3D(x.Value, y.Value, z)); return result;
    }
    // TODO: bulge(42) 미처리 — 호 구간이 직선 현으로 단순화되어 곡관 진단 결과에 영향 가능
    private static List<Point3D> ReadVertices(IReadOnlyList<(string Code, string Value)> pairs)
    {
        var result = new List<Point3D>();
        for (var i = 0; i < pairs.Count; i++) if (pairs[i] == ("0", "VERTEX"))
        {
            double? x = null, y = null; var z = 0d; i++;
            while (i < pairs.Count && pairs[i].Code != "0") { if (pairs[i].Code == "10") x = Number(pairs[i].Value); else if (pairs[i].Code == "20") y = Number(pairs[i].Value); else if (pairs[i].Code == "30") z = Number(pairs[i].Value); i++; }
            if (x.HasValue && y.HasValue) result.Add(new Point3D(x.Value, y.Value, z)); i--;
        }
        return result;
    }
    private static IReadOnlyDictionary<string, string> ReadAttributes(IReadOnlyList<(string Code, string Value)> pairs)
    {
        var attributes = new Dictionary<string, string>(CadXDataAttributeParser.Parse(pairs.Where(x => x.Code == "1000").Select(x => x.Value)), StringComparer.OrdinalIgnoreCase);
        var layer = pairs.FirstOrDefault(x => x.Code == "8").Value;
        if (!string.IsNullOrWhiteSpace(layer)) attributes["LAYER"] = layer;
        return attributes;
    }
    private static double Number(string value) => double.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
}

internal static class CadXDataAttributeParser
{
    public static IReadOnlyDictionary<string, string> Parse(IEnumerable<string> values)
    {
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var text in values)
        foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var keyValue = part.Split('=', 2);
            if (keyValue.Length == 2) attributes[keyValue[0].Trim().ToUpperInvariant()] = keyValue[1].Trim();
        }
        return attributes;
    }
}
