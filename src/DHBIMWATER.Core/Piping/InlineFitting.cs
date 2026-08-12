namespace DHBIMWATER.Core.Piping;

/// <summary>엣지 내부에 순서대로 배치되는 부속품이다.</summary>
/// <param name="FamilyTypeName">배치할 FamilySymbol("패밀리명 : 타입명"). 사용자가 아직 선택하지 않았으면 빈 문자열.</param>
public sealed record InlineFitting(Guid Id, string TypeKey, string FamilyTypeName, double T, int Order)
{
    public InlineFitting(string typeKey, string familyTypeName, double t, int order)
        : this(Guid.NewGuid(), typeKey, familyTypeName, Math.Clamp(t, 0, 1), order) { }
}
