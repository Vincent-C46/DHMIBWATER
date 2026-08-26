namespace DHBIMWATER.Core.Gis;

/// <summary>
/// 주철관 핸드북의 덕타일주철관 <b>등급</b> 목록. 직관 제원의 두께 조회에만 사용한다.
/// 이름은 "관종"이지만 실제로는 등급이며, 관종 자체는 <see cref="PipeMaterial"/>가 담당한다.
/// </summary>
// TODO: 강관·PE·PVC 등급이 추가될 때 PipeKindCatalog → PipeGradeCatalog로 개명 검토.
//       지금 개명하면 PipeAlignment·Repository·파라미터 매핑까지 광범위하게 번져 기능 이득 없이 변경만 커진다.
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

    /// <summary>해당 관종에서 고를 수 있는 등급. 규격 자료가 없는 관종은 빈 목록이다.</summary>
    public static IReadOnlyList<string> AllFor(PipeMaterial material)
        => material == PipeMaterial.DuctileIron ? All : Array.Empty<string>();

    /// <summary>등급 → 관종. 목록에 없는 명칭이면 null(관종을 추측하지 않는다).</summary>
    public static PipeMaterial? MaterialOf(string? pipeKind)
        => Normalize(pipeKind) is null ? null : PipeMaterial.DuctileIron;

    /// <summary>공백만 정리해 고정 목록에 정확히 대응한다. 알 수 없는 명칭은 추측하지 않는다.</summary>
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var compact = string.Concat(value.Where(x => !char.IsWhiteSpace(x)));
        return All.FirstOrDefault(x => string.Equals(string.Concat(x.Where(c => !char.IsWhiteSpace(c))), compact, StringComparison.OrdinalIgnoreCase));
    }
}
