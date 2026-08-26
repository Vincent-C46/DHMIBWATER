using DHBIMWATER.Core.Piping;
using Xunit;

namespace DHBIMWATER.Core.Tests.Piping;

public class PipeSegmentPlanTests
{
    [Fact]
    public void Exact_multiple_of_straight_length_produces_only_straights()
    {
        var result = PipeSegmentPlan.Build(12000, []);

        Assert.Empty(result.Warnings);
        Assert.Equal(2, result.Segments.Count);
        Assert.All(result.Segments, x => Assert.Equal(PipeSegmentKind.Straight, x.Kind));
        Assert.Equal([3000, 9000], result.Segments.Select(x => x.CenterMm));
    }

    [Fact]
    public void Remainder_becomes_a_single_short_pipe_at_the_end()
    {
        var result = PipeSegmentPlan.Build(14000, []);

        Assert.Empty(result.Warnings);
        Assert.Equal(3, result.Segments.Count);
        Assert.Equal(PipeSegmentKind.Straight, result.Segments[0].Kind);
        Assert.Equal(PipeSegmentKind.Straight, result.Segments[1].Kind);

        var last = result.Segments[2];
        Assert.Equal(PipeSegmentKind.Short, last.Kind);
        Assert.Equal(2000, last.LengthMm);
        Assert.Equal(13000, last.CenterMm);   // 12000 ~ 14000 의 중점
    }

    [Fact]
    public void Edge_shorter_than_straight_length_becomes_one_short_pipe()
    {
        var result = PipeSegmentPlan.Build(2500, []);

        var segment = Assert.Single(result.Segments);
        Assert.Equal(PipeSegmentKind.Short, segment.Kind);
        Assert.Equal(2500, segment.LengthMm);
        Assert.Equal(1250, segment.CenterMm);
    }

    [Fact]
    public void Build_SplitsByCustomStraightLength()
    {
        var result = PipeSegmentPlan.Build(1750, [], straightLengthMm: 500);

        Assert.Empty(result.Errors);
        Assert.Equal(4, result.Segments.Count);
        Assert.Equal(3, result.Segments.Count(x => x.Kind == PipeSegmentKind.Straight));
        Assert.All(result.Segments.Take(3), x => Assert.Equal(500, x.LengthMm));
        var shortPipe = result.Segments[3];
        Assert.Equal(PipeSegmentKind.Short, shortPipe.Kind);
        Assert.Equal(250, shortPipe.LengthMm);
    }

    [Fact]
    public void Node_fitting_trims_are_excluded_from_both_ends()
    {
        // 양 끝 절점의 곡관 몸통 300mm씩을 뺀 6600mm 가용 구간 → 직관 1본 + 단관 600mm
        var result = PipeSegmentPlan.Build(7200, [new OccupiedSpan(0, 300), new OccupiedSpan(6900, 7200)]);

        Assert.Empty(result.Warnings);
        Assert.Equal(2, result.Segments.Count);
        Assert.Equal(PipeSegmentKind.Straight, result.Segments[0].Kind);
        Assert.Equal(3300, result.Segments[0].CenterMm);   // 300 ~ 6300

        Assert.Equal(PipeSegmentKind.Short, result.Segments[1].Kind);
        Assert.Equal(600, result.Segments[1].LengthMm);
        Assert.Equal(6600, result.Segments[1].CenterMm);   // 6300 ~ 6900
    }

    [Fact]
    public void Inline_valve_splits_the_edge_and_each_gap_is_filled_independently()
    {
        // 밸브가 7000~7400 을 점유 → 앞 구간 7000(직관+단관), 뒤 구간 2600(단관)
        var result = PipeSegmentPlan.Build(10000, [new OccupiedSpan(7000, 7400)]);

        Assert.Empty(result.Warnings);
        Assert.Equal(3, result.Segments.Count);
        Assert.Equal(PipeSegmentKind.Straight, result.Segments[0].Kind);
        Assert.Equal(3000, result.Segments[0].CenterMm);

        Assert.Equal(PipeSegmentKind.Short, result.Segments[1].Kind);
        Assert.Equal(1000, result.Segments[1].LengthMm);
        Assert.Equal(6500, result.Segments[1].CenterMm);   // 6000 ~ 7000

        Assert.Equal(PipeSegmentKind.Short, result.Segments[2].Kind);
        Assert.Equal(2600, result.Segments[2].LengthMm);
        Assert.Equal(8700, result.Segments[2].CenterMm);   // 7400 ~ 10000
    }

    [Fact]
    public void Remainder_below_minimum_is_reported_as_an_error()
    {
        var result = PipeSegmentPlan.Build(6000.5, []);

        var segment = Assert.Single(result.Segments);
        Assert.Equal(PipeSegmentKind.Straight, segment.Kind);
        Assert.Contains(result.Errors, x => x.Contains("최소 단관 길이"));
    }

    [Fact]
    public void Overlapping_occupied_spans_are_reported()
    {
        var result = PipeSegmentPlan.Build(10000, [new OccupiedSpan(0, 3000), new OccupiedSpan(2000, 4000)]);

        Assert.Contains(result.Errors, x => x.Contains("겹칩니다"));
        // 순수 계산 결과는 진단용으로 남기지만 Revit Repo는 Errors가 있으면 전체 트랜잭션을 롤백한다.
        Assert.Equal(6000, result.Segments.Sum(x => x.LengthMm));
    }

    [Fact]
    public void Fully_occupied_edge_produces_no_segments()
    {
        var result = PipeSegmentPlan.Build(500, [new OccupiedSpan(0, 300), new OccupiedSpan(200, 500)]);

        Assert.Empty(result.Segments);
    }

    [Fact]
    public void Occupied_spans_outside_the_edge_are_reported_before_clamping()
    {
        var result = PipeSegmentPlan.Build(5000, [new OccupiedSpan(-1000, 1000), new OccupiedSpan(4000, 9000)]);

        Assert.Equal(2, result.Errors.Count);
        Assert.All(result.Errors, x => Assert.Contains("벗어납니다", x));
        var segment = Assert.Single(result.Segments);
        Assert.Equal(3000, segment.LengthMm);
        Assert.Equal(2500, segment.CenterMm);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    public void Invalid_edge_length_throws(double edgeLengthMm)
        => Assert.Throws<ArgumentOutOfRangeException>(() => PipeSegmentPlan.Build(edgeLengthMm, []));
}
