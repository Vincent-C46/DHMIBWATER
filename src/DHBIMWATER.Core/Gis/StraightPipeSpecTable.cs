namespace DHBIMWATER.Core.Gis;

/// <summary>직관 전용 규격. 곡관 벽두께와 허용굴곡에는 관여하지 않는다.</summary>
/// <param name="DiameterMm">호칭지름 DN. 조회는 Exact Match다.</param>
public sealed record StraightPipeSpec(string PipeKind, double DiameterMm, double OuterDiameterMm, double ThicknessMm);

public sealed class StraightPipeSpecTable
{
    private const double Epsilon = 1e-6;

    public StraightPipeSpecTable(IReadOnlyList<StraightPipeSpec> entries) => Entries = entries;

    public IReadOnlyList<StraightPipeSpec> Entries { get; }

    /// <summary>정확히 일치하는 (관종, DN)이 없으면 null을 반환한다.</summary>
    public StraightPipeSpec? Find(string pipeKind, double diameterMm) => Entries.FirstOrDefault(x =>
        string.Equals(x.PipeKind, pipeKind, StringComparison.OrdinalIgnoreCase)
        && Math.Abs(x.DiameterMm - diameterMm) <= Epsilon);

    /// <summary>같은 DN 행의 OD 불일치를 반환한다.</summary>
    public IReadOnlyList<double> FindOuterDiameterConflicts() => Entries
        .Where((entry, index) => Entries.Skip(index + 1).Any(other =>
            Math.Abs(entry.DiameterMm - other.DiameterMm) <= Epsilon
            && Math.Abs(entry.OuterDiameterMm - other.OuterDiameterMm) > Epsilon))
        .Select(x => x.DiameterMm)
        .Distinct()
        .OrderBy(x => x)
        .ToList();

    // ── 주철관 핸드북(2020년판) 출처: docs/주철관핸드북(2020년판)_DN.pdf ──────────────
    // 직관부의 실바깥지름(DE)·관두께(e)만 옮겼다. 조인트 무게·소켓 제원은 사용처가 없어 제외.

    /// <summary>DN → DE. 7개 관종 전부 동일한 실바깥지름을 쓴다.</summary>
    private static readonly (double Dn, double De)[] Diameters =
    {
        (80, 98), (100, 118), (125, 144), (150, 170), (200, 222), (250, 274),
        (300, 326), (350, 378), (400, 429), (450, 480), (500, 532), (600, 635),
        (700, 738), (800, 842), (900, 945), (1000, 1048), (1100, 1144), (1200, 1255)
    };

    /// <summary><see cref="Diameters"/>의 DN600 위치. 상수 4종관은 DN600부터 수록된다.</summary>
    private const int Dn600Index = 11;

    // 핸드북상 상수 2종관과 하수 1종관, 상수 3종관과 하수 2종관은 제원이 완전히 동일하다.
    private static readonly double[] WaterClass1 =
    {
        7.4, 7.5, 7.6, 7.7, 7.8, 8.3, 8.8, 9.4, 9.9,
        10.5, 11.0, 12.1, 13.2, 14.3, 15.4, 16.5, 17.6, 18.7
    };

    private static readonly double[] WaterClass2 =
    {
        6.7, 6.8, 6.9, 7.0, 7.1, 7.5, 8.0, 8.5, 9.0,
        9.5, 10.0, 11.0, 12.0, 13.0, 14.0, 15.0, 16.0, 17.0
    };

    private static readonly double[] WaterClass3 =
    {
        6.0, 6.1, 6.2, 6.3, 6.4, 6.8, 7.2, 7.7, 8.1,
        8.6, 9.0, 9.9, 10.8, 11.7, 12.6, 13.5, 14.4, 15.3
    };

    /// <summary>DN600~1200만 존재한다. 소구경 상수 4종관은 핸드북에 없다.</summary>
    private static readonly double[] WaterClass4 = { 8.8, 9.6, 10.4, 11.2, 12.0, 12.8, 13.6 };

    private static readonly double[] SewerClass3 =
    {
        4.7, 4.8, 4.8, 4.9, 5.0, 5.3, 5.6, 6.0, 6.3,
        6.7, 7.0, 7.7, 8.4, 9.1, 9.8, 10.5,
        // 핸드북 원문 그대로. DN1100·1200은 앞 구간 증가폭에서 벗어나 상수 4종관과 같은 값이다.
        12.8, 13.6
        // 참고: DN100과 DN125가 둘 다 4.8로 같은 것도 원문 그대로다.
    };

    private static IEnumerable<StraightPipeSpec> Build(string pipeKind, IReadOnlyList<double> thicknessMm, int startIndex = 0) =>
        thicknessMm.Select((thickness, offset) =>
        {
            var (dn, de) = Diameters[startIndex + offset];
            return new StraightPipeSpec(pipeKind, dn, de, thickness);
        });

    public static StraightPipeSpecTable Default { get; } = new(new[]
        {
            Build(PipeKindCatalog.Water1, WaterClass1),
            Build(PipeKindCatalog.Water2, WaterClass2),
            Build(PipeKindCatalog.Water3, WaterClass3),
            Build(PipeKindCatalog.Water4, WaterClass4, Dn600Index),
            Build(PipeKindCatalog.Sewer1, WaterClass2),
            Build(PipeKindCatalog.Sewer2, WaterClass3),
            Build(PipeKindCatalog.Sewer3, SewerClass3)
        }
        .SelectMany(x => x)
        .ToList());
}
