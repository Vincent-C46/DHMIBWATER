using DHBIMWATER.Application.DTOs.Revit.PumpingStation;

namespace DHBIMWATER.Application.UseCases.AutoGenerator
{
    public class ParseValveExtensionUseCase
    {
        private const string SheetName = "밸브 연장";

        public Dictionary<double, PumpValveExtensionDto> Execute(IReadOnlyDictionary<string, List<string[]>> allSheets)
        {
            var result = new Dictionary<double, PumpValveExtensionDto>();
            if (!allSheets.TryGetValue(SheetName, out var rows)) return result;

            // Row 8 (index 7)부터: A열=관경(D), D열=총연장, K열=밸브연장(역지밸브 추가 시)
            // 밸브받침 제원: 역지밸브 없음=G,H,I,J열(6~9), 역지밸브 있음=N,O,P,Q열(13~16)
            foreach (var row in rows.Skip(7))
            {
                if (!ExcelCellParser.TryParseNumber(row.ElementAtOrDefault(0), out var d)) continue;

                ExcelCellParser.TryParseNumber(row.ElementAtOrDefault(3), out var totalExtension);
                ExcelCellParser.TryParseNumber(row.ElementAtOrDefault(10), out var valveExtension);

                var withoutCheckValve = ParseValveDimension(row, 6);  // G,H,I,J
                var withCheckValve = ParseValveDimension(row, 13);    // N,O,P,Q

                result[d] = new PumpValveExtensionDto(totalExtension, valveExtension, withoutCheckValve, withCheckValve);
            }

            return result;
        }

        // startCol부터 연속 4개 컬럼(폭/길이/높이/배치)을 밸브받침 제원으로 파싱
        private static PumpValveDimensionDto ParseValveDimension(string[] row, int startCol)
        {
            ExcelCellParser.TryParseNumber(row.ElementAtOrDefault(startCol), out var width);
            ExcelCellParser.TryParseNumber(row.ElementAtOrDefault(startCol + 1), out var length);
            ExcelCellParser.TryParseNumber(row.ElementAtOrDefault(startCol + 2), out var height);
            ExcelCellParser.TryParseNumber(row.ElementAtOrDefault(startCol + 3), out var placement);
            return new PumpValveDimensionDto(width, length, height, placement);
        }
    }
}
