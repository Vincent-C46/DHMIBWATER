namespace DHBIMWATER.Core.Gis;

/// <summary>선형 소스의 직경·관종 값을 배치되는 패밀리(빔, 추후 3점 가변패밀리 등)의 실제 인스턴스 파라미터에
/// 자동으로 연결하기 위한 후보 파라미터명 매칭. 후보에 없으면 사용자가 UI에서 직접 매핑한다.</summary>
public static class FamilyParameterMapper
{
    // TODO: 프로젝트에서 실제 사용하는 구조 프레임/가변패밀리 파라미터명을 확인해 후보를 조정할 것.
    private static readonly HashSet<string> DiameterParameterCandidates = new(StringComparer.OrdinalIgnoreCase)
        { "직경", "관경", "구경", "Diameter", "DIAMETER", "D" };
    private static readonly HashSet<string> KindParameterCandidates = new(StringComparer.OrdinalIgnoreCase)
        { "관종", "재질", "재료", "종류", "PipeKind", "Material" };

    public static string? GuessDiameterParameter(IEnumerable<string> parameterNames) => parameterNames.FirstOrDefault(DiameterParameterCandidates.Contains);
    public static string? GuessKindParameter(IEnumerable<string> parameterNames) => parameterNames.FirstOrDefault(KindParameterCandidates.Contains);
}
