namespace DHBIMWATER.Core.Piping;

/// <summary>엣지 내부에 순서대로 배치되는 부속품이다.</summary>
public sealed record InlineFitting(Guid Id, string TypeKey, double T, int Order)
{
    public InlineFitting(string typeKey, double t, int order)
        : this(Guid.NewGuid(), typeKey, Math.Clamp(t, 0, 1), order) { }
}
