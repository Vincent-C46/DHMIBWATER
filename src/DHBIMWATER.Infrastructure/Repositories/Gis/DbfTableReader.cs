using System.Text;
using System.IO;
using DHBIMWATER.Application.DTOs.Gis;

namespace DHBIMWATER.Infrastructure.Repositories.Gis;

internal sealed record DbfReadData(IReadOnlyList<ShapefileFieldInfo> Fields, IReadOnlyList<IReadOnlyDictionary<string, string>> Rows, string EncodingName);

internal static class DbfTableReader
{
    public static DbfReadData Read(string path, string? cpgPath)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding = ResolveEncoding(cpgPath);
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);
        reader.ReadByte(); reader.ReadBytes(3);
        var recordCount = reader.ReadInt32();
        var headerLength = reader.ReadInt16(); var recordLength = reader.ReadInt16();
        stream.Position = 29; reader.ReadByte();
        stream.Position = 32;
        var fields = new List<ShapefileFieldInfo>();
        while (stream.Position < headerLength)
        {
            var first = reader.ReadByte();
            if (first == 0x0D) break;
            var descriptor = new byte[32]; descriptor[0] = first;
            var remainder = reader.ReadBytes(31);
            if (remainder.Length != 31) throw new EndOfStreamException("DBF 필드 디스크립터가 불완전합니다.");
            Buffer.BlockCopy(remainder, 0, descriptor, 1, 31);
            var name = Encoding.ASCII.GetString(descriptor, 0, 11).TrimEnd('\0', ' ');
            fields.Add(new ShapefileFieldInfo(name, (char)descriptor[11], descriptor[16], descriptor[17]));
        }
        stream.Position = headerLength;
        var rows = new List<IReadOnlyDictionary<string, string>>();
        for (var row = 0; row < recordCount && stream.Position + recordLength <= stream.Length; row++)
        {
            var bytes = reader.ReadBytes(recordLength);
            if (bytes.Length != recordLength) break;
            if (bytes[0] == 0x2A) continue;
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var offset = 1;
            foreach (var field in fields)
            {
                values[field.Name] = encoding.GetString(bytes, offset, field.Length).Trim();
                offset += field.Length;
            }
            rows.Add(values);
        }
        return new DbfReadData(fields, rows, encoding.WebName);
    }

    private static Encoding ResolveEncoding(string? cpgPath)
    {
        var name = !string.IsNullOrWhiteSpace(cpgPath) && File.Exists(cpgPath) ? File.ReadAllText(cpgPath).Trim().ToUpperInvariant() : "949";
        return name switch { "949" or "EUC-KR" or "CP949" => Encoding.GetEncoding(949), "65001" or "UTF8" or "UTF-8" => Encoding.UTF8, _ => Encoding.GetEncoding(949) };
    }
}
