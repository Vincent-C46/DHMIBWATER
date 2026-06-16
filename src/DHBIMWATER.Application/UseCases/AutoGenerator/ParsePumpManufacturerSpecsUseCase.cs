using DHBIMWATER.Application.DTOs.Revit.PumpingStation;

namespace DHBIMWATER.Application.UseCases.AutoGenerator
{
    public class ParsePumpManufacturerSpecsUseCase
    {
        /// <summary>
        /// 전체 시트 딕셔너리와 제작사 이름 목록을 받아
        /// 제작사별 (D, HD) → SpecDto 딕셔너리를 반환한다.
        /// </summary>
        public Dictionary<string, Dictionary<(double D, double HD), PumpManufacturerSpecDto>> Execute(
            IReadOnlyDictionary<string, List<string[]>> allSheets,
            IEnumerable<string> manufacturerNames)
        {
            var result = new Dictionary<string, Dictionary<(double, double), PumpManufacturerSpecDto>>();

            foreach (var name in manufacturerNames)
            {
                if (!allSheets.TryGetValue(name, out var rows)) continue;
                result[name] = ParseManufacturerSheet(rows);
            }


            return result;
        }

        private static Dictionary<(double D, double HD), PumpManufacturerSpecDto> ParseManufacturerSheet(List<string[]> rows)
        {
            var result = new Dictionary<(double, double), PumpManufacturerSpecDto>();

            if (rows.Count < 6) return result;

            // Row 5 (index 4): HD 헤더 컬럼 탐지 — "5m", "7m", "10m" 패턴, 3열 간격
            var headerRow = rows[4];
            var hdColumns = new List<(double HD, int ColIdx)>();

            for (int col = 2; col < headerRow.Length; col++)
            {
                var cell = headerRow[col]?.Trim() ?? "";
                if (cell.Length > 1 && char.ToLower(cell[^1]) == 'm' &&
                    double.TryParse(cell[..^1], out var hdVal))
                {
                    hdColumns.Add((hdVal, col));
                }
            }

            if (hdColumns.Count == 0) return result;

            double currentD = 0;

            // Row 6(index 5)부터 3행 묶음으로 읽기
            for (int i = 5; i + 2 < rows.Count; i += 3)
            {
                var row0 = rows[i];         // 개구부형상
                var row1 = rows[i + 1];     // B5
                var row2 = rows[i + 2];     // 받침블록

                // A열에서 D값 갱신 (묶음 첫 행에만 존재)
                if (row0.Length > 0 && !string.IsNullOrWhiteSpace(row0[0]) &&
                    double.TryParse(row0[0], out var d))
                {
                    currentD = d;
                }

                if (currentD <= 0) continue;

                foreach (var (hd, colIdx) in hdColumns)
                {
                    var openingShape = colIdx < row0.Length ? row0[colIdx]?.Trim() ?? "" : "";
                    if (string.IsNullOrWhiteSpace(openingShape)) continue;

                    double b5 = 0;
                    if (row1.Length > colIdx)
                        double.TryParse(row1[colIdx], out b5);

                    // 받침블록: colIdx=값, colIdx+1="x", colIdx+2=값
                    double sbWidth = 0, sbHeight = 0;
                    if (row2.Length > colIdx)
                        double.TryParse(row2[colIdx], out sbWidth);
                    if (row2.Length > colIdx + 2)
                        double.TryParse(row2[colIdx + 2], out sbHeight);

                    result[(currentD, hd)] = new PumpManufacturerSpecDto(openingShape, b5, sbWidth, sbHeight);
                }
            }

            return result;
        }
    }
}
