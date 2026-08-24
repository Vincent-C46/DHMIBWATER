using System.Globalization;
using System.IO;
using System.Text;
using ClosedXML.Excel;
using ExcelDataReader;
using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Gis;

namespace DHBIMWATER.Infrastructure.Repositories.Gis;

/// <summary>코리더 점 보고서 같은 엑셀·CSV 좌표표를 관로 선형으로 읽는다. 시트 1개 = 관로 1개(행마다 정점), 열 매핑은 사용자가 지정한다.</summary>
public sealed class ExcelAlignmentReader : IExcelAlignmentSourceReader
{
    // ExcelDataReader로 CSV를 읽으려면 CP949 같은 레거시 코드페이지를 먼저 등록해야 한다(DbfTableReader와 동일한 전제).
    static ExcelAlignmentReader() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public bool CanRead(string filePath) => IsExcel(filePath) || IsCsv(filePath);

    public IReadOnlyList<string> GetSheetNames(string filePath)
    {
        using var workbook = OpenWorkbook(filePath);
        return workbook.Worksheets.Select(x => x.Name).ToList();
    }

    public IReadOnlyList<IReadOnlyList<string?>> PreviewRows(string filePath, string sheetName, int maxRows)
    {
        using var workbook = OpenWorkbook(filePath);
        var sheet = FindSheet(workbook, sheetName);
        var used = sheet.RangeUsed();
        if (used is null) return Array.Empty<IReadOnlyList<string?>>();
        var lastRow = Math.Min(used.LastRowUsed().RowNumber(), maxRows);
        var lastColumn = used.LastColumnUsed().ColumnNumber();
        var rows = new List<IReadOnlyList<string?>>();
        for (var r = 1; r <= lastRow; r++)
        {
            var row = new List<string?>(lastColumn);
            for (var c = 1; c <= lastColumn; c++) row.Add(sheet.Cell(r, c).GetFormattedString());
            rows.Add(row);
        }
        return rows;
    }

    public ShapefileReadResult Read(string filePath) => throw new InvalidOperationException("엑셀·CSV 소스는 열 매핑이 필요합니다. Read(filePath, mapping)을 사용하세요.");

    public ShapefileReadResult Read(string filePath, ExcelAlignmentMapping mapping)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException("엑셀·CSV 파일을 찾을 수 없습니다.", filePath);
        using var workbook = OpenWorkbook(filePath);
        var sheet = FindSheet(workbook, mapping.SheetName);
        var used = sheet.RangeUsed();
        var lastRow = used?.LastRowUsed().RowNumber() ?? 0;

        var vertices = new List<Point3D>();
        var warnings = new List<string>();
        double? previousStation = null;
        var stationOutOfOrder = 0;

        for (var r = mapping.DataStartRow; r <= lastRow; r++)
        {
            if (!TryReadDouble(sheet.Cell(r, mapping.XColumnIndex + 1), out var x) ||
                !TryReadDouble(sheet.Cell(r, mapping.YColumnIndex + 1), out var y) ||
                !TryReadDouble(sheet.Cell(r, mapping.ZColumnIndex + 1), out var z))
            {
                warnings.Add($"{mapping.SheetName} {r}행: X·Y·Z 값을 읽지 못해 건너뛰었습니다.");
                continue;
            }
            vertices.Add(new Point3D(x, y, z));

            if (mapping.StationColumnIndex is { } stationColumn)
            {
                var station = ParseStation(sheet.Cell(r, stationColumn + 1).GetString());
                if (station is not null)
                {
                    if (previousStation is not null && station < previousStation) stationOutOfOrder++;
                    previousStation = station;
                }
            }
        }

        if (stationOutOfOrder > 0) warnings.Add($"{mapping.SheetName}: 측점(Station)이 행 순서와 다르게 역행하는 구간이 {stationOutOfOrder}건 있습니다.");
        if (vertices.Count < 2) warnings.Add($"{mapping.SheetName}: 유효한 정점이 {vertices.Count}개뿐이라 관로를 구성할 수 없습니다.");

        // 헤더행의 열 이름을 필드 목록으로 만들어, 메인 그리드의 직경·관종 필드 콤보박스가 SHP와 같은 방식으로 동작하게 한다.
        // 시트 전체 = 관로 1개(레코드 1개)라 열마다 값은 1개만 실린다 — 데이터 영역에서 처음 만난 공백 아닌 값을 대표값으로 쓴다.
        var (fields, attributes) = ReadHeaderFields(sheet, mapping, used?.LastColumnUsed().ColumnNumber() ?? 0, lastRow, warnings);

        var features = vertices.Count >= 2
            ? new List<PipeAlignment> { new(vertices, string.Empty, 0, Path.GetFileName(filePath), "1", attributes) }
            : new List<PipeAlignment>();

        var extent = vertices.Count == 0 ? ShapefileExtent.Empty
            : new ShapefileExtent(vertices.Min(x => x.X), vertices.Min(x => x.Y), vertices.Max(x => x.X), vertices.Max(x => x.Y), vertices.Min(x => x.Z), vertices.Max(x => x.Z));

        return new ShapefileReadResult(features, fields, extent, null, null, "Excel", features.Count, vertices.Count, Math.Max(0, vertices.Count - 1), warnings, attributes.Count > 0 ? attributes : null);
    }

    /// <summary>
    /// 헤더행에서 필드 목록과, 열마다 대표값 1개를 담은 속성 딕셔너리를 만든다.
    /// X·Y·Z로 이미 소비한 열은 직경·관종 후보가 될 수 없어 제외하고, 측점 열은 이름 필드로 쓰는 표가 있어 남긴다.
    /// 엑셀은 DBF 같은 타입 정보가 없으므로 전부 문자형('C')으로 취급한다 — Loader·파서 모두 문자열을 파싱한다.
    /// </summary>
    private static (List<ShapefileFieldInfo> Fields, Dictionary<string, string> Attributes) ReadHeaderFields(
        IXLWorksheet sheet, ExcelAlignmentMapping mapping, int lastColumn, int lastRow, List<string> warnings)
    {
        var fields = new List<ShapefileFieldInfo>();
        var attributes = new Dictionary<string, string>();
        var coordinateColumns = new HashSet<int> { mapping.XColumnIndex + 1, mapping.YColumnIndex + 1, mapping.ZColumnIndex + 1 };

        for (var c = 1; c <= lastColumn; c++)
        {
            if (coordinateColumns.Contains(c)) continue;
            var name = sheet.Cell(mapping.HeaderRow, c).GetFormattedString().Trim();
            if (string.IsNullOrWhiteSpace(name)) continue;
            // Attributes가 딕셔너리라 헤더 이름이 겹치면 예외가 난다. 뒤에 오는 열에 _2, _3 …을 붙여 유일하게 만든다.
            if (fields.Any(x => string.Equals(x.Name, name, StringComparison.Ordinal)))
            {
                var suffix = 2;
                while (fields.Any(x => string.Equals(x.Name, $"{name}_{suffix}", StringComparison.Ordinal))) suffix++;
                name = $"{name}_{suffix}";
            }
            fields.Add(new ShapefileFieldInfo(name, 'C', 254, 0));

            string? representative = null;
            var distinct = new HashSet<string>(StringComparer.Ordinal);
            for (var r = mapping.DataStartRow; r <= lastRow; r++)
            {
                // 셀 서식 그대로 읽는다 — DN300·주철관 같은 문자 표기가 보존되어야 규격 매핑 탭의 원본 표기가 맞는다.
                var value = sheet.Cell(r, c).GetFormattedString().Trim();
                if (string.IsNullOrWhiteSpace(value)) continue;
                representative ??= value;
                distinct.Add(value);
            }
            if (representative is null) continue;
            attributes[name] = representative;

            // 측점은 원래 행마다 달라 경고 대상이 아니다. 그 외 열에 값이 섞이면 대표값 1개만 쓰인다는 사실을 알린다.
            var isStationColumn = mapping.StationColumnIndex is { } station && station + 1 == c;
            if (!isStationColumn && distinct.Count > 1)
                warnings.Add($"{mapping.SheetName}: '{name}' 열에 서로 다른 값이 {distinct.Count}종 있습니다. 시트당 값 1개만 적용되어 '{representative}'을 사용합니다.");
        }

        return (fields, attributes);
    }

    private static bool IsExcel(string filePath) => string.Equals(Path.GetExtension(filePath), ".xlsx", StringComparison.OrdinalIgnoreCase);
    private static bool IsCsv(string filePath) => string.Equals(Path.GetExtension(filePath), ".csv", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// CSV는 시트 개념이 없어 파일명을 시트 이름 하나로 쓴다. 매핑 대화상자의 시트 목록도 이 이름 1개만 보여준다.
    /// 워크시트 이름 규칙(31자 이내, []:*?/\ 금지)을 어기면 ClosedXML이 예외를 던지므로 미리 정규화한다.
    /// </summary>
    public static string CsvSheetName(string filePath)
    {
        var name = Path.GetFileNameWithoutExtension(filePath);
        foreach (var invalid in new[] { '[', ']', ':', '*', '?', '/', '\\' }) name = name.Replace(invalid, '_');
        if (name.Length > 31) name = name[..31];
        return string.IsNullOrWhiteSpace(name) ? "CSV" : name;
    }

    /// <summary>
    /// CSV는 ClosedXML이 직접 열지 못해, ExcelDataReader로 읽어 메모리 워크북 1시트로 옮긴 뒤
    /// 이후 미리보기·매핑·좌표 파싱을 엑셀과 완전히 동일한 경로로 태운다.
    /// </summary>
    private static XLWorkbook OpenWorkbook(string filePath)
    {
        if (!IsCsv(filePath)) return new XLWorkbook(filePath);

        using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateCsvReader(stream, new ExcelReaderConfiguration { FallbackEncoding = DetectFallbackEncoding(filePath) });
        var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(CsvSheetName(filePath));
        var row = 0;
        while (reader.Read())
        {
            row++;
            for (var c = 0; c < reader.FieldCount; c++)
            {
                var text = reader.GetValue(c)?.ToString();
                if (string.IsNullOrWhiteSpace(text)) continue;
                // 숫자로 읽히는 칸은 숫자로 넣어야 TryReadDouble이 문자열 파싱 폴백을 타지 않는다.
                if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)) sheet.Cell(row, c + 1).Value = number;
                else sheet.Cell(row, c + 1).Value = text;
            }
        }
        return workbook;
    }

    /// <summary>BOM이 없는 CSV에 쓸 인코딩. 국내 CSV는 CP949가 흔하지만, BOM 없는 UTF-8도 많아 엄격 디코딩으로 먼저 판별한다.</summary>
    private static Encoding DetectFallbackEncoding(string filePath)
    {
        try
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var buffer = new byte[Math.Min(64 * 1024, stream.Length)];
            var read = stream.Read(buffer, 0, buffer.Length);
            // 마지막 멀티바이트 문자가 잘려 오탐하지 않도록 끝의 후속 바이트(10xxxxxx)는 잘라낸다.
            while (read > 0 && (buffer[read - 1] & 0xC0) == 0x80) read--;
            new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(buffer, 0, read);
            return Encoding.UTF8;
        }
        catch (DecoderFallbackException) { return Encoding.GetEncoding(949); }
        catch (IOException) { return Encoding.GetEncoding(949); }
    }

    private static IXLWorksheet FindSheet(XLWorkbook workbook, string sheetName)
        => workbook.Worksheets.FirstOrDefault(x => string.Equals(x.Name, sheetName, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"'{sheetName}' 시트를 찾을 수 없습니다.");

    private static bool TryReadDouble(IXLCell cell, out double value)
    {
        if (cell.TryGetValue(out double d)) { value = d; return true; }
        return double.TryParse(cell.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    // 체이니지 "0+000.00"(km+m) 또는 순수 숫자를 모두 허용한다. 실패하면 정렬 검증만 건너뛴다.
    private static double? ParseStation(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var text = raw.Trim();
        var plus = text.IndexOf('+');
        if (plus <= 0) return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : null;
        var kmPart = text[..plus];
        var mPart = text[(plus + 1)..];
        return double.TryParse(kmPart, NumberStyles.Float, CultureInfo.InvariantCulture, out var km) && double.TryParse(mPart, NumberStyles.Float, CultureInfo.InvariantCulture, out var m)
            ? km * 1000 + m
            : null;
    }
}
