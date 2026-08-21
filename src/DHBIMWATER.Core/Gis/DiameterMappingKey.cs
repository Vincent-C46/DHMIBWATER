namespace DHBIMWATER.Core.Gis;

public static class DiameterMappingKey
{
    /// <summary>직경 원본값이 비어 있는 레코드를 한 행으로 묶는 키. 표시 문자열이 곧 사전 키라 양쪽이 반드시 공유해야 한다.</summary>
    public const string Empty = "(값 없음)";

    public static string Of(string? rawValue)
        => string.IsNullOrWhiteSpace(rawValue) ? Empty : rawValue.Trim();
}
