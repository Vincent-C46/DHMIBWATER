using System.Buffers.Binary;
using System.IO;
using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Core.Geometry;

namespace DHBIMWATER.Infrastructure.Repositories.Gis;

internal sealed record ShpRecord(int RecordNumber, IReadOnlyList<IReadOnlyList<Point3D>> Parts);
internal sealed record ShpReadData(IReadOnlyList<ShpRecord> Records, ShapefileExtent Extent, int ShapeType);

internal static class ShpGeometryReader
{
    public static ShpReadData Read(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);
        if (ReadInt32BigEndian(reader) != 9994) throw new InvalidDataException("SHP 파일 코드가 9994가 아닙니다.");
        stream.Position = 24;
        var fileLength = ReadInt32BigEndian(reader) * 2L;
        stream.Position = 28;
        if (reader.ReadInt32() != 1000) throw new InvalidDataException("지원하지 않는 SHP 버전입니다.");
        var shapeType = reader.ReadInt32();
        if (shapeType is not 3 and not 13) throw new InvalidDataException($"지원하지 않는 SHP 형식입니다. PolyLine(3) 또는 PolyLineZ(13)만 허용됩니다. (현재: {shapeType})");
        var extent = new ShapefileExtent(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble());
        stream.Position = 100;
        var records = new List<ShpRecord>();
        var limit = Math.Min(fileLength, stream.Length);
        while (stream.Position + 8 <= limit)
        {
            var recordNumber = ReadInt32BigEndian(reader);
            var contentBytes = ReadInt32BigEndian(reader) * 2L;
            if (contentBytes < 4 || stream.Position + contentBytes > limit) throw new InvalidDataException($"SHP 레코드 {recordNumber} 길이가 올바르지 않습니다.");
            var end = stream.Position + contentBytes;
            var recordShapeType = reader.ReadInt32();
            if (recordShapeType == 0) { stream.Position = end; continue; }
            if (recordShapeType is not 3 and not 13) throw new InvalidDataException($"레코드 {recordNumber}의 형식 {recordShapeType}은 지원하지 않습니다.");
            reader.ReadBytes(32); // Box
            var partCount = reader.ReadInt32();
            var pointCount = reader.ReadInt32();
            if (partCount < 0 || pointCount < 0) throw new InvalidDataException($"레코드 {recordNumber}의 정점 수가 올바르지 않습니다.");
            var parts = Enumerable.Range(0, partCount).Select(_ => reader.ReadInt32()).ToArray();
            var points = new Point3D[pointCount];
            for (var i = 0; i < pointCount; i++) points[i] = new Point3D(reader.ReadDouble(), reader.ReadDouble(), 0);
            if (recordShapeType == 13)
            {
                if (stream.Position + 16 + pointCount * 8L > end) throw new InvalidDataException($"레코드 {recordNumber}에 Z 배열이 없습니다.");
                reader.ReadDouble(); reader.ReadDouble();
                for (var i = 0; i < pointCount; i++) points[i] = points[i] with { Z = reader.ReadDouble() };
            }
            var split = new List<IReadOnlyList<Point3D>>();
            for (var i = 0; i < parts.Length; i++)
            {
                var start = parts[i]; var count = (i + 1 < parts.Length ? parts[i + 1] : pointCount) - start;
                if (start < 0 || count < 0 || start + count > pointCount) throw new InvalidDataException($"레코드 {recordNumber}의 Parts 배열이 올바르지 않습니다.");
                split.Add(points.Skip(start).Take(count).ToArray());
            }
            records.Add(new ShpRecord(recordNumber, split));
            stream.Position = end; // M 배열 유무와 무관하게 content length 끝으로 이동
        }
        return new ShpReadData(records, extent, shapeType);
    }

    private static int ReadInt32BigEndian(BinaryReader reader)
    {
        Span<byte> bytes = stackalloc byte[4];
        if (reader.Read(bytes) != 4) throw new EndOfStreamException();
        return BinaryPrimitives.ReadInt32BigEndian(bytes);
    }
}
