using System.Globalization;
using System.IO;
using ClosedXML.Excel;
using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Gis;

namespace DHBIMWATER.Infrastructure.Repositories.Gis;

/// <summary>코리더 점 보고서 같은 엑셀 좌표표를 관로 선형으로 읽는다. 시트 1개 = 관로 1개(행마다 정점), 열 매핑은 사용자가 지정한다.</summary>
public sealed class ExcelAlignmentReader : IExcelAlignmentSourceReader
{
    public bool CanRead(string filePath) => string.Equals(Path.GetExtension(filePath), ".xlsx", StringComparison.OrdinalIgnoreCase);

    public IReadOnlyList<string> GetSheetNames(string filePath)
    {
        using var workbook = new XLWorkbook(filePath);
        return workbook.Worksheets.Select(x => x.Name).ToList();
    }

    public IReadOnlyList<IReadOnlyList<string?>> PreviewRows(string filePath, string sheetName, int maxRows)
    {
        using var workbook = new XLWorkbook(filePath);
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

    public ShapefileReadResult Read(string filePath) => throw new InvalidOperationException("엑셀 소스는 열 매핑이 필요합니다. Read(filePath, mapping)을 사용하세요.");

    public ShapefileReadResult Read(string filePath, ExcelAlignmentMapping mapping)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException("엑셀 파일을 찾을 수 없습니다.", filePath);
        using var workbook = new XLWorkbook(filePath);
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

        var attributes = new Dictionary<string, string>();
        var fields = new List<ShapefileFieldInfo>();

        var features = vertices.Count >= 2
            ? new List<PipeAlignment> { new(vertices, string.Empty, 0, Path.GetFileName(filePath), "1", attributes) }
            : new List<PipeAlignment>();

        var extent = vertices.Count == 0 ? ShapefileExtent.Empty
            : new ShapefileExtent(vertices.Min(x => x.X), vertices.Min(x => x.Y), vertices.Max(x => x.X), vertices.Max(x => x.Y), vertices.Min(x => x.Z), vertices.Max(x => x.Z));

        return new ShapefileReadResult(features, fields, extent, null, null, "Excel", features.Count, vertices.Count, Math.Max(0, vertices.Count - 1), warnings, attributes.Count > 0 ? attributes : null);
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
