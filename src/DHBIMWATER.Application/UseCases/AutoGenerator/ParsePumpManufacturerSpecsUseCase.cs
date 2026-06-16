using DHBIMWATER.Application.DTOs.Revit.PumpingStation;

namespace DHBIMWATER.Application.UseCases.AutoGenerator
{
    public class ParsePumpManufacturerSpecsUseCase
    {
        public Dictionary<string, Dictionary<(double D, double HD), PumpManufacturerSpecDto>> Execute(
            IReadOnlyDictionary<string, List<string[]>> allSheets,
            IEnumerable<string> manufacturerNames)
        {
            var result = new Dictionary<string, Dictionary<(double, double), PumpManufacturerSpecDto>>();

            foreach (var name in manufacturerNames)
            {
                // 1. 정확한 시트명 매칭 → 실패 시 공백 제거 후 재시도
                if (!allSheets.TryGetValue(name, out var rows))
                {
                    var normalized = name.Replace(" ", "");
                    var match = allSheets.Keys.FirstOrDefault(k => k.Replace(" ", "") == normalized);
                    if (match == null) continue;
                    rows = allSheets[match];
                }

                result[name] = ParseManufacturerSheet(rows);
            }

            return result;
        }

        private static Dictionary<(double D, double HD), PumpManufacturerSpecDto> ParseManufacturerSheet(List<string[]> rows)
        {
            var result = new Dictionary<(double, double), PumpManufacturerSpecDto>();

            if (rows.Count < 6) return result;

            // Row 5 (index 4): HD 헤더 컬럼 탐지 — "5m", "7m", "10m" 패턴
            var headerRow = rows[4];
            var hdColumns = new List<(double HD, int ColIdx)>();

            for (int col = 2; col < headerRow.Length; col++)
            {
                var cell = ExcelCellParser.Clean(headerRow[col]);
                if (cell.Length > 1 && char.ToLower(cell[^1]) == 'm' &&
                    double.TryParse(cell[..^1], out var hdVal))
                {
                    hdColumns.Add((hdVal, col));
                }
            }

            if (hdColumns.Count == 0) return result;

            double currentD = 0;

            // Row 6 (index 5)부터 3행 묶음으로 읽기
            for (int i = 5; i + 2 < rows.Count; i += 3)
            {
                var row0 = rows[i];         // 개구부형상
                var row1 = rows[i + 1];     // B5
                var row2 = rows[i + 2];     // 받침블록

                // A열에서 D값 갱신 (묶음 첫 행에만 존재)
                if (row0.Length > 0 && ExcelCellParser.TryParseNumber(row0[0], out var d))
                    currentD = d;

                if (currentD <= 0) continue;

                foreach (var (hd, colIdx) in hdColumns)
                {
                    var openingShape = colIdx < row0.Length ? row0[colIdx]?.Trim() ?? "" : "";
                    if (string.IsNullOrWhiteSpace(openingShape)) continue;

                    // B5: 해당 HD 컬럼 우선, null/0이면 첫 번째 유효값 fallback
                    if (!ExcelCellParser.TryParseNumber(row1.ElementAtOrDefault(colIdx), out var b5) || b5 == 0)
                    {
                        foreach (var (_, fallbackIdx) in hdColumns)
                        {
                            if (ExcelCellParser.TryParseNumber(row1.ElementAtOrDefault(fallbackIdx), out var fallback) && fallback != 0)
                            {
                                b5 = fallback;
                                break;
                            }
                        }
                    }

                    // 받침블록: colIdx=값, colIdx+1="x", colIdx+2=값
                    ExcelCellParser.TryParseNumber(row2.ElementAtOrDefault(colIdx), out var sbWidth);
                    ExcelCellParser.TryParseNumber(row2.ElementAtOrDefault(colIdx + 2), out var sbHeight);

                    result[(currentD, hd)] = new PumpManufacturerSpecDto(openingShape, b5, sbWidth, sbHeight);
                }
            }

            return result;
        }
    }
}
