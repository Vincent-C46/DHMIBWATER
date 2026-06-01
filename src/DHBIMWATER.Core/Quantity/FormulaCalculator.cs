using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DHBIMWATER.Core.Quantity
{
    public static class FormulaCalculator
    {
        // 계산
        public static double Calculate(string formula, Dictionary<string, double> varDict)
        {
            var expr = Regex.Replace(formula, @"\bx\b", "*", RegexOptions.IgnoreCase);
            // 전각 특수문자 치환
            expr = expr.Replace("×", "*");
            expr = expr.Replace("÷", "/");
            expr = expr.Replace("－", "-");
            expr = expr.Replace("＋", "+");

            // 제곱 구현
            expr = Regex.Replace(expr, @"(\w+)\^(\d+)", m =>
            {
                var baseVal = m.Groups[1].Value;
                var exp = int.Parse(m.Groups[2].Value);
                return string.Join("*", Enumerable.Repeat(baseVal, exp));
            });

            // PI 치환
            expr = expr.Replace("PI", Math.PI.ToString(CultureInfo.InvariantCulture));
            expr = expr.Replace("π", Math.PI.ToString(CultureInfo.InvariantCulture));

            foreach (var (key, value) in varDict)
                expr = Regex.Replace(expr, $@"\b{key}\b", value.ToString(System.Globalization.CultureInfo.InvariantCulture));

            return Convert.ToDouble(new DataTable().Compute(expr, null));
        }

        // 산식 렌더
        public static string Render(string formula, Dictionary<string, double> varDict)
        {
            var result = formula;
            foreach (var (key, value) in varDict)
            {
                var formatted = value == Math.Floor(value)
                    ? ((int)value).ToString()        // 정수면 그대로
                    : value.ToString("F2");          // 소수면 F2

                result = Regex.Replace(result, $@"\b{key}\b", $"{formatted}({key})");
            }


            return result;
        }
    }
}