using System.Globalization;
using System.Text.RegularExpressions;

namespace DHBIMWATER.Core.Gis;

/// <summary>직경 필드 값 파싱 결과. Kind는 값에서 관종이 분리된 경우에만 채워진다.</summary>
public readonly record struct DiameterParseResult(bool Success, double DiameterMm, string? Kind);

public static class AlignmentAttributeParser
{
    // GIS 납품 데이터는 관종과 관경을 한 셀에 합쳐 보내는 경우가 많다(예: "상수_D300").
    // 1NF 위반이지만 원본을 바꿀 수 없으므로 이 경계에서 분해해 내부 모델(PipeKind/DiameterMm)은 정규화한다.
    // 구분자는 언더바·공백·하이픈을 모두 허용하고, D 접두는 있어도 없어도 된다.
    private static readonly Regex KindAndDiameterPattern = new("^(?<kind>.*?)[\\s_-]*[Dd](?<dia>\\d+(?:\\.\\d+)?)\\s*(?:mm|밀리)?$", RegexOptions.Compiled);
    // D 없이 구분자만 있는 형식(예: "상수_300", "PVC 300"). 관경은 최소 2자리여야 한다 —
    // "밸브실_2" 같은 일련번호를 관경으로 오인하지 않기 위한 최소 방어선이다.
    private static readonly Regex SeparatedDiameter = new("^(?<kind>.*?)[\\s_-]+(?<dia>\\d{2,}(?:\\.\\d+)?)\\s*(?:mm|밀리)?$", RegexOptions.Compiled);
    private static readonly Regex NumericDiameter = new("^[Dd]?(?<dia>\\d+(?:\\.\\d+)?)\\s*(?:mm|밀리)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    // 관종이 뒤에 붙는 형식(예: "D300주철", "300 PVC"). mm·밀리 단위 표기는 관종이 아니므로 제외한다.
    private static readonly Regex SuffixedKind = new("^[Dd]?(?<dia>\\d+(?:\\.\\d+)?)[\\s_-]*(?<kind>(?!mm$|밀리$)[가-힣A-Za-z][가-힣A-Za-z0-9]*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    // PT_PL_PDM / PIPE_KN_NM은 국가상수도정보시스템 납품 SHP의 실제 필드명이다(DBF 확인).
    // 해당 데이터의 관경 값은 "100"과 "300.00"이 한 필드에 섞여 들어온다 — 아래 패턴이 둘 다 받는다.
    private static readonly HashSet<string> DiameterFields = new(StringComparer.OrdinalIgnoreCase)
        { "Diameter", "DIAMETER", "DIA", "PIP_DIA", "PIPE_DIA", "PT_PL_PDM", "구경", "관경", "호구경", "HOGU" };
    private static readonly HashSet<string> KindFields = new(StringComparer.OrdinalIgnoreCase)
        { "KIND", "MTRL", "MATERIAL", "PIPE_KIND", "PIPE_KN_NM", "관종", "재질", "재료" };

    public static DiameterParseResult ParseDiameter(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new DiameterParseResult(false, 0, null);
        var value = raw.Trim();
        // 순수 숫자("300", "D300", "300mm")를 먼저 걸러야 뒤의 관종 분리 패턴이 오작동하지 않는다.
        var numeric = NumericDiameter.Match(value);
        if (numeric.Success) return new DiameterParseResult(true, Parse(numeric), null);
        var combined = KindAndDiameterPattern.Match(value);
        if (combined.Success)
            return new DiameterParseResult(true, Parse(combined), NormalizeKind(combined.Groups["kind"].Value));
        var separated = SeparatedDiameter.Match(value);
        if (separated.Success)
            return new DiameterParseResult(true, Parse(separated), NormalizeKind(separated.Groups["kind"].Value));
        var suffixed = SuffixedKind.Match(value);
        if (suffixed.Success)
            return new DiameterParseResult(true, Parse(suffixed), NormalizeKind(suffixed.Groups["kind"].Value));
        // 해석 실패 시 Kind를 채우면 원본 문자열이 통째로 관종명이 된다(예: PipeKind = "PVC D300").
        // 클래스 계약대로 분리에 성공한 경우에만 Kind를 채우고, 실패 시에는 호출부의 수동 관종으로 넘긴다.
        return new DiameterParseResult(false, 0, null);
    }

    public static string? GuessDiameterField(IEnumerable<string> fieldNames) => fieldNames.FirstOrDefault(DiameterFields.Contains);
    public static string? GuessKindField(IEnumerable<string> fieldNames) => fieldNames.FirstOrDefault(KindFields.Contains);

    private static double Parse(Match match) => double.Parse(match.Groups["dia"].Value, CultureInfo.InvariantCulture);
    private static string? NormalizeKind(string kind)
    {
        var trimmed = kind.Trim().Trim('_', '-', ' ');
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
