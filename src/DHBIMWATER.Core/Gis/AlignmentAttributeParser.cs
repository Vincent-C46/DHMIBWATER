using System.Globalization;
using System.Text.RegularExpressions;

namespace DHBIMWATER.Core.Gis;

/// <summary>직경 필드 값 파싱 결과. Kind는 값에서 관종이 분리된 경우에만 채워진다.</summary>
public readonly record struct DiameterParseResult(bool Success, double DiameterMm, string? Kind);

public static class AlignmentAttributeParser
{
    private static readonly Regex KindAndDiameterPattern = new("^(?<kind>.*?)_D(?<dia>\\d+(?:\\.\\d+)?)$", RegexOptions.Compiled);
    private static readonly Regex PrefixedDiameter = new("^[Dd](?<dia>\\d+(?:\\.\\d+)?)$", RegexOptions.Compiled);
    private static readonly Regex NumericDiameter = new("^(?<dia>\\d+(?:\\.\\d+)?)\\s*(?:mm|밀리)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly HashSet<string> DiameterFields = new(StringComparer.OrdinalIgnoreCase)
        { "Diameter", "DIAMETER", "DIA", "PIP_DIA", "PIPE_DIA", "구경", "관경", "호구경", "HOGU" };
    private static readonly HashSet<string> KindFields = new(StringComparer.OrdinalIgnoreCase)
        { "KIND", "MTRL", "MATERIAL", "PIPE_KIND", "관종", "재질", "재료" };

    public static DiameterParseResult ParseDiameter(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new DiameterParseResult(false, 0, null);
        var value = raw.Trim();
        var combined = KindAndDiameterPattern.Match(value);
        if (combined.Success)
            return new DiameterParseResult(true, Parse(combined), NormalizeKind(combined.Groups["kind"].Value));
        var prefixed = PrefixedDiameter.Match(value);
        if (prefixed.Success) return new DiameterParseResult(true, Parse(prefixed), null);
        var numeric = NumericDiameter.Match(value);
        if (numeric.Success) return new DiameterParseResult(true, Parse(numeric), null);
        return new DiameterParseResult(false, 0, raw);
    }

    public static string? GuessDiameterField(IEnumerable<string> fieldNames) => fieldNames.FirstOrDefault(DiameterFields.Contains);
    public static string? GuessKindField(IEnumerable<string> fieldNames) => fieldNames.FirstOrDefault(KindFields.Contains);

    private static double Parse(Match match) => double.Parse(match.Groups["dia"].Value, CultureInfo.InvariantCulture);
    private static string? NormalizeKind(string kind) => string.IsNullOrWhiteSpace(kind) ? null : kind;
}
