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
            foreach (var row in rows.Skip(7))
            {
                if (!ExcelCellParser.TryParseNumber(row.ElementAtOrDefault(0), out var d)) continue;

                ExcelCellParser.TryParseNumber(row.ElementAtOrDefault(3), out var totalExtension);
                ExcelCellParser.TryParseNumber(row.ElementAtOrDefault(10), out var valveExtension);

                result[d] = new PumpValveExtensionDto(totalExtension, valveExtension);
            }

            return result;
        }
    }
}
