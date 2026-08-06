using DHBIMWATER.Core.Geometry;

namespace DHBIMWATER.Core.Piping;

/// <summary>
/// 밸브실 외곽 벽체 한 장이다. 좌표는 mm이며 <see cref="InnerStart"/>·<see cref="InnerEnd"/>가
/// 실내측 면(기준면)이고, <see cref="OuterStart"/>·<see cref="OuterEnd"/>는 두께 표시용 실외측 면이다.
/// </summary>
public sealed record OutlineWall(
    long ElementId,
    Point2D InnerStart,
    Point2D InnerEnd,
    Point2D OuterStart,
    Point2D OuterEnd,
    double ThicknessMm);

/// <summary>외곽선까지의 임시치수 한 방향. <see cref="Hit"/>는 외곽선 위의 도달점이다.</summary>
public readonly record struct OutlineMeasure(Point2D From, Point2D Hit, double DistanceMm);

/// <summary>
/// Revit에서 피킹한 외곽 벽체들로 구성한 밸브실 레이아웃이다.
/// 모든 계산은 벽 <b>내측면</b>을 기준으로 한다(사용자 확정 사항).
/// 순수 기하 계산만 수행하며 Revit·UI에 의존하지 않는다.
/// </summary>
public sealed class ValveRoomOutline
{
    /// <summary>레이 캐스팅·스냅 판정에 쓰는 허용오차(mm).</summary>
    public const double Tolerance = 0.5;

    public static ValveRoomOutline Empty { get; } = new([]);

    public ValveRoomOutline(IReadOnlyList<OutlineWall> walls)
    {
        Walls = walls;
        if (walls.Count == 0) return;

        var xs = walls.SelectMany(w => new[] { w.InnerStart.X, w.InnerEnd.X }).ToList();
        var ys = walls.SelectMany(w => new[] { w.InnerStart.Y, w.InnerEnd.Y }).ToList();
        MinX = xs.Min(); MaxX = xs.Max(); MinY = ys.Min(); MaxY = ys.Max();
    }

    public IReadOnlyList<OutlineWall> Walls { get; }
    public bool IsEmpty => Walls.Count == 0;
    public double MinX { get; }
    public double MaxX { get; }
    public double MinY { get; }
    public double MaxY { get; }
    public double WidthMm => MaxX - MinX;
    public double HeightMm => MaxY - MinY;

    /// <summary>내측면 끝점 전체의 평균. 캔버스 기준점 자동 설정에 쓴다.</summary>
    public Point2D Centroid => IsEmpty
        ? new Point2D(0, 0)
        : new Point2D(
            Walls.Average(w => (w.InnerStart.X + w.InnerEnd.X) / 2),
            Walls.Average(w => (w.InnerStart.Y + w.InnerEnd.Y) / 2));

    /// <summary>모든 내측면 좌표를 <paramref name="offset"/>만큼 평행이동한 새 외곽을 만든다.</summary>
    public ValveRoomOutline Translate(Point2D offset)
    {
        if (IsEmpty) return Empty;
        return new ValveRoomOutline(Walls.Select(w => w with
        {
            InnerStart = Shift(w.InnerStart, offset),
            InnerEnd = Shift(w.InnerEnd, offset),
            OuterStart = Shift(w.OuterStart, offset),
            OuterEnd = Shift(w.OuterEnd, offset),
        }).ToList());
    }

    private static Point2D Shift(Point2D p, Point2D offset) => new(p.X + offset.X, p.Y + offset.Y);

    /// <summary>
    /// <paramref name="from"/>에서 좌·우로 수평 레이를 쏴 가장 가까운 내측면까지의 거리를 구한다.
    /// L자·요철 평면에서도 실제로 마주 보는 변을 찾는다. 도달하는 변이 없으면 null.
    /// </summary>
    public OutlineMeasure? MeasureHorizontal(Point2D from) => Nearest(from, HorizontalHits(from));

    /// <summary><see cref="MeasureHorizontal"/>의 수직 방향 버전이다.</summary>
    public OutlineMeasure? MeasureVertical(Point2D from) => Nearest(from, VerticalHits(from));

    private IEnumerable<Point2D> HorizontalHits(Point2D from)
    {
        foreach (var wall in Walls)
        {
            var a = wall.InnerStart; var b = wall.InnerEnd;
            var dy = b.Y - a.Y;
            if (Math.Abs(dy) <= Tolerance) continue;   // 수평 벽은 수평 레이와 평행하므로 제외
            var t = (from.Y - a.Y) / dy;
            if (t < 0 || t > 1) continue;
            yield return new Point2D(a.X + t * (b.X - a.X), from.Y);
        }
    }

    private IEnumerable<Point2D> VerticalHits(Point2D from)
    {
        foreach (var wall in Walls)
        {
            var a = wall.InnerStart; var b = wall.InnerEnd;
            var dx = b.X - a.X;
            if (Math.Abs(dx) <= Tolerance) continue;   // 수직 벽은 수직 레이와 평행하므로 제외
            var t = (from.X - a.X) / dx;
            if (t < 0 || t > 1) continue;
            yield return new Point2D(from.X, a.Y + t * (b.Y - a.Y));
        }
    }

    private static OutlineMeasure? Nearest(Point2D from, IEnumerable<Point2D> hits)
    {
        OutlineMeasure? best = null;
        foreach (var hit in hits)
        {
            var distance = from.DistanceTo(hit);
            if (distance <= Tolerance) continue;       // 변 위에 올라선 경우는 치수 표시 생략
            if (best is null || distance < best.Value.DistanceMm) best = new OutlineMeasure(from, hit, distance);
        }
        return best;
    }

    /// <summary>내측면 끝점 목록. OSNAP 끝점 후보로 쓴다.</summary>
    public IEnumerable<Point2D> Endpoints()
    {
        foreach (var wall in Walls) { yield return wall.InnerStart; yield return wall.InnerEnd; }
    }

    /// <summary>내측면 중간점 목록. OSNAP 중간점 후보로 쓴다.</summary>
    public IEnumerable<Point2D> Midpoints()
    {
        foreach (var wall in Walls)
            yield return new Point2D((wall.InnerStart.X + wall.InnerEnd.X) / 2, (wall.InnerStart.Y + wall.InnerEnd.Y) / 2);
    }

    /// <summary><paramref name="point"/>에서 각 내측면에 내린 수선의 발 목록. OSNAP 근처점 후보로 쓴다.</summary>
    public IEnumerable<Point2D> NearestPoints(Point2D point)
    {
        foreach (var wall in Walls)
        {
            var t = Math.Clamp(Segment2D.ParameterOnSegment(point, wall.InnerStart, wall.InnerEnd), 0, 1);
            yield return new Point2D(
                wall.InnerStart.X + (wall.InnerEnd.X - wall.InnerStart.X) * t,
                wall.InnerStart.Y + (wall.InnerEnd.Y - wall.InnerStart.Y) * t);
        }
    }

    /// <summary>IN 화살표가 붙을 좌측 내측면의 X. 내측면 전체의 최소 X를 좌측 벽면으로 본다.</summary>
    public double LeftFaceX => IsEmpty ? 0 : MinX;

    /// <summary>OUT 화살표가 붙을 우측 내측면의 X. 내측면 전체의 최대 X를 우측 벽면으로 본다.</summary>
    public double RightFaceX => IsEmpty ? 0 : MaxX;
}
