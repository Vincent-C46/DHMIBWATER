namespace DHBIMWATER.Core.Gis;

/// <summary>적용 규격이 정의하는 이형관 형식 구분. Joint Type과는 별개다.</summary>
public enum BendForm { AType, BType }

/// <param name="DiameterMm">호칭지름 DN. 조회는 Exact Match다.</param>
/// <param name="WallThicknessMm">e — 곡관 자체의 규격상 벽두께(mm).</param>
/// <param name="TypeName">선택한 곡관 패밀리 안의 타입명.</param>
public sealed record BendFittingEntry(
    double DiameterMm,
    double AngleDeg,
    BendForm Form,
    double LayingLengthMm,
    double CenterlineRadiusMm,
    double ExtraLegLengthMm = 0d,
    double WallThicknessMm = 0d,
    string? TypeName = null);

public sealed class BendFittingCatalog
{
    private const double Epsilon = 1e-6;

    public BendFittingCatalog(IReadOnlyList<BendFittingEntry> entries) => Entries = entries;
    public IReadOnlyList<BendFittingEntry> Entries { get; }

    public BendFittingEntry? Find(double diameterMm, double angleDeg, BendForm form) => Entries.FirstOrDefault(x =>
        x.Form == form
        && Math.Abs(x.DiameterMm - diameterMm) <= Epsilon
        && Math.Abs(x.AngleDeg - angleDeg) <= Epsilon);

    // TODO: 주철관 핸드북 수치 미확보
    public static BendFittingCatalog Default { get; } = new(Array.Empty<BendFittingEntry>());
}

/// <summary>관로 규격과 판정 설정의 저장 단위.</summary>
public sealed record BendSettings(
    StraightPipeSpecTable StraightPipes,
    JointDeflectionTable JointDeflections,
    BendFittingCatalog Fittings,
    string ActiveJointType,
    JointApplicationMode ApplicationMode)
{
    public static BendSettings Default { get; } = new(
        StraightPipeSpecTable.Default,
        JointDeflectionTable.Default,
        BendFittingCatalog.Default,
        JointTypeCatalog.KpMechanical,
        JointApplicationMode.SingleJoint);
}
