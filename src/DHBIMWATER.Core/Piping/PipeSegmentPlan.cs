namespace DHBIMWATER.Core.Piping;

/// <summary>엣지 축(시작점부터의 거리, mm) 위에서 관을 놓을 수 없는 구간 하나.
/// 절점 부속(곡관·T형)의 몸통과 인라인 밸브류의 몸통이 여기에 해당한다.</summary>
public readonly record struct OccupiedSpan(double StartMm, double EndMm);

public enum PipeSegmentKind
{
    /// <summary>규격 길이(기본 6m) 직관.</summary>
    Straight,
    /// <summary>규격 길이에 못 미치는 나머지. 길이 파라미터로 구동한다.</summary>
    Short
}

/// <summary>배치할 관 조각 하나. 거리는 모두 엣지 시작점 기준(mm)이고, 삽입점이 관 중앙이므로 중점만 넘긴다.</summary>
public sealed record PipeSegmentPlacement(PipeSegmentKind Kind, double CenterMm, double LengthMm);

public sealed record PipeSegmentPlanResult(
    IReadOnlyList<PipeSegmentPlacement> Segments,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors);

/// <summary>
/// 엣지 하나를 직관·단관 조각으로 분해한다(docs/39 §3). Revit 의존성이 없는 순수 계산이다.
/// 절점 부속·밸브가 차지하는 구간을 빼고 남은 빈 구간마다, 앞에서부터 규격 길이 직관을 채우고
/// 마지막 나머지를 단관 하나로 만든다.
/// </summary>
public static class PipeSegmentPlan
{
    /// <summary>직관 1본의 규격 길이(mm). 주철관 표준 정척.</summary>
    public const double StraightLengthMm = 6000;
    /// <summary>이보다 짧은 나머지는 단관으로 만들지 않고 경고만 남긴다.</summary>
    public const double MinSegmentMm = 10;
    private const double Tolerance = 1e-6;

    public static PipeSegmentPlanResult Build(
        double edgeLengthMm,
        IEnumerable<OccupiedSpan> occupied,
        double straightLengthMm = StraightLengthMm,
        double minSegmentMm = MinSegmentMm)
    {
        if (!double.IsFinite(edgeLengthMm) || edgeLengthMm <= 0)
            throw new ArgumentOutOfRangeException(nameof(edgeLengthMm), "엣지 길이는 0보다 큰 유한한 값이어야 합니다.");
        if (!double.IsFinite(straightLengthMm) || straightLengthMm <= 0)
            throw new ArgumentOutOfRangeException(nameof(straightLengthMm), "직관 길이는 0보다 큰 유한한 값이어야 합니다.");
        if (!double.IsFinite(minSegmentMm) || minSegmentMm < 0)
            throw new ArgumentOutOfRangeException(nameof(minSegmentMm), "최소 조각 길이는 0 이상이어야 합니다.");

        var segments = new List<PipeSegmentPlacement>();
        var warnings = new List<string>();
        var errors = new List<string>();
        var ordered = occupied
            .Select(x => x.StartMm <= x.EndMm ? x : new OccupiedSpan(x.EndMm, x.StartMm))
            .OrderBy(x => x.StartMm)
            .ToList();

        foreach (var span in ordered)
            if (span.StartMm < -Tolerance || span.EndMm > edgeLengthMm + Tolerance)
                errors.Add(FormattableString.Invariant(
                    $"부속 점유 구간({span.StartMm:N0}~{span.EndMm:N0}mm)이 배관 길이(0~{edgeLengthMm:N0}mm)를 벗어납니다. 부속을 선 가운데 쪽으로 옮기세요."));

        // 겹침은 클램프하기 전 원본 좌표로 잡아야 어디가 문제인지 알려줄 수 있다.
        for (var i = 1; i < ordered.Count; i++)
            if (ordered[i].StartMm < ordered[i - 1].EndMm - Tolerance)
                errors.Add(FormattableString.Invariant(
                    $"부속이 차지하는 구간이 겹칩니다({ordered[i - 1].StartMm:N0}~{ordered[i - 1].EndMm:N0}mm ↔ {ordered[i].StartMm:N0}~{ordered[i].EndMm:N0}mm). 부속을 서로 떨어뜨리거나 배관을 더 길게 그리세요."));

        var cursor = 0.0;
        foreach (var span in ordered)
        {
            var start = Math.Clamp(span.StartMm, 0, edgeLengthMm);
            var end = Math.Clamp(span.EndMm, 0, edgeLengthMm);
            if (start > cursor + Tolerance) Fill(cursor, start);
            cursor = Math.Max(cursor, end);
        }
        if (edgeLengthMm > cursor + Tolerance) Fill(cursor, edgeLengthMm);

        return new PipeSegmentPlanResult(segments, warnings, errors);

        void Fill(double from, double to)
        {
            var gap = to - from;
            if (gap <= Tolerance) return;

            // 부동소수 오차로 12000mm가 직관 1본 + 단관 6000mm로 쪼개지지 않도록 허용오차를 더해 센다.
            var straightCount = (int)Math.Floor((gap + Tolerance) / straightLengthMm);
            var head = from;
            for (var i = 0; i < straightCount; i++)
            {
                segments.Add(new PipeSegmentPlacement(PipeSegmentKind.Straight, head + straightLengthMm / 2, straightLengthMm));
                head += straightLengthMm;
            }

            var remainder = to - head;
            if (remainder <= Tolerance) return;
            if (remainder < minSegmentMm)
            {
                errors.Add(FormattableString.Invariant(
                    $"남은 {remainder:N1}mm 구간은 최소 단관 길이({minSegmentMm:N0}mm)보다 짧습니다. 연결이 끊기지 않도록 선 길이나 부속 위치를 조정하세요."));
                return;
            }
            segments.Add(new PipeSegmentPlacement(PipeSegmentKind.Short, head + remainder / 2, remainder));
        }
    }
}
