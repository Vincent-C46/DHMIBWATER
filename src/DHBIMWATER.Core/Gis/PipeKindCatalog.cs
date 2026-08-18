namespace DHBIMWATER.Core.Gis;

/// <summary>주철관 핸드북 관종. 직관 제원의 두께 조회에만 사용한다.</summary>
public static class PipeKindCatalog
{
    public const string Water1 = "상수 1종관";
    public const string Water2 = "상수 2종관";
    public const string Water3 = "상수 3종관";
    public const string Water4 = "상수 4종관";
    public const string Sewer1 = "하수 1종관";
    public const string Sewer2 = "하수 2종관";
    public const string Sewer3 = "하수 3종관";

    public static IReadOnlyList<string> All { get; } = new[]
    {
        Water1, Water2, Water3, Water4, Sewer1, Sewer2, Sewer3
    };

    /// <summary>공백만 정리해 고정 목록에 정확히 대응한다. 알 수 없는 명칭은 추측하지 않는다.</summary>
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var compact = string.Concat(value.Where(x => !char.IsWhiteSpace(x)));
        return All.FirstOrDefault(x => string.Equals(string.Concat(x.Where(c => !char.IsWhiteSpace(c))), compact, StringComparison.OrdinalIgnoreCase));
    }
}
