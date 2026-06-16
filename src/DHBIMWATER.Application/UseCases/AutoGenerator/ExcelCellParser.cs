namespace DHBIMWATER.Application.UseCases.AutoGenerator
{
    // "1,000 " 같은 포맷 정리 후 파싱 — 시트 파싱 UseCase 공용
    internal static class ExcelCellParser
    {
        public static bool TryParseNumber(string? raw, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(raw)) return false;
            return double.TryParse(Clean(raw), out value);
        }

        public static string Clean(string? s) => (s ?? "").Replace(",", "").Trim();
    }
}
