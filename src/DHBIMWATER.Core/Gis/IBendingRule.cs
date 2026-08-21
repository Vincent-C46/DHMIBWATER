namespace DHBIMWATER.Core.Gis;

/// <summary>
/// 관종별 곡관 판정 규칙. 관종마다 판정 <b>근거 자체</b>가 다르기 때문에
/// 허용굴곡값 하나를 바꿔 끼우는 방식이 아니라 규칙 구현을 갈아 끼운다.
/// <list type="bullet">
///   <item>덕타일주철관 — 조인트 허용굴곡 + 표준 곡관 (<see cref="JointDeflectionRule"/>)</item>
///   <item>강관 — 제작곡관 규격</item>
///   <item>PE관 — 최소곡률반경(절점마다 이형관을 넣는 모델 자체가 성립하지 않음)</item>
/// </list>
/// </summary>
public interface IBendingRule
{
    PipeMaterial Material { get; }

    /// <summary>Bend 절점 하나에 들어갈 곡관을 판정한다.</summary>
    BendResolution Resolve(NodeClassification node, BendSettings settings);
}

/// <summary>관종에 맞는 판정 규칙을 고른다.</summary>
public static class BendingRuleCatalog
{
    private static readonly IReadOnlyList<IBendingRule> Rules = new IBendingRule[]
    {
        new JointDeflectionRule()
        // 강관·PE·PVC 규칙은 해당 관종의 규격 자료와 배치 로직이 확보될 때 추가한다.
        // 빈 구현을 미리 두면 판정이 조용히 잘못된 결과를 내므로 만들지 않는다.
    };

    public static IBendingRule For(PipeMaterial material)
        => Rules.FirstOrDefault(x => x.Material == material)
           ?? throw new NotSupportedException($"'{material}' 관종의 곡관 판정 규칙이 아직 없습니다. 현재는 덕타일주철관만 지원합니다.");
}
