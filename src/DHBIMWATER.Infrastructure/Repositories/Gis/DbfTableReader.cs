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
            // 필드명도 레코드 값과 같은 코드페이지다. 국가 수치지도 DBF는 "구분"·"등고수치"처럼 한글 필드명을 쓴다.
            // ASCII로 읽으면 비ASCII 바이트가 전부 '?'가 되어 필드 콤보·샘플 표시가 "????"로 깨지고,
            // AlignmentAttributeParser의 한글 후보("구경"·"관종" 등)도 영영 매칭되지 않는다.
            // NUL 뒤 잔여 바이트가 2바이트 문자로 오결합되지 않도록 NUL 앞까지만 디코딩한다.
            var nameLength = Array.IndexOf(descriptor, (byte)0, 0, 11);
            if (nameLength < 0) nameLength = 11;
            var name = encoding.GetString(descriptor, 0, nameLength).TrimEnd(' ');
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
