using System.Text.RegularExpressions;
using System.Data;

namespace DHBIMWATER.Core.Quantity
{
    public static class FormulaCalculator
    {
        // 계산
        public static double Calculate(string formula, Dictionary<string, double> varDict)
        {
            var expr = Regex.Replace(formula, @"\bx\b", "*", RegexOptions.IgnoreCase);
            foreach (var (key, value) in varDict)
                expr = Regex.Replace(expr, $@"\b{key}\b", value.ToString());

            return Convert.ToDouble(new DataTable().Compute(expr, null));
        }

        // 렌더링
        public static string Render(string formula, Dictionary<string, double> varDict)
        {
            var result = formula;

            foreach (var (key, value) in varDict)
                result = Regex.Replace(result, $@"\b{key}\b", $"{value:F2}({key})");

            return result;
        }
    }
}
