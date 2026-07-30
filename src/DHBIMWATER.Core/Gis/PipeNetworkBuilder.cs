using DHBIMWATER.Core.Geometry;

namespace DHBIMWATER.Core.Gis;

/// <summary>
/// 여러 관로 폴리선을 하나의 위상 그래프로 조립한다.
/// 폴리선 끝점이 다른 폴리선 선분 위에 있으면 그 선분을 분할해 T분기(차수 3)를,
/// 두 폴리선이 교차하면 양쪽을 분할해 십자(차수 4)를 만든다.
/// 좌표와 허용오차는 같은 단위를 사용한다(원본 GIS 좌표 = m).
/// </summary>
public static class PipeNetworkBuilder
{
    /// <summary>UI 기본 스냅 허용오차 10mm를 m 단위로 표현한 값.</summary>
    public const double DefaultSnapToleranceM = 0.01;

    public static PipeNetworkGraph Build(IReadOnlyList<PipeAlignment> alignments, double snapTolerance = DefaultSnapToleranceM)
    {
        if (snapTolerance <= 0) throw new ArgumentOutOfRangeException(nameof(snapTolerance), "스냅 허용오차는 0보다 커야 합니다.");

        var segments = CollectSegments(alignments, snapTolerance);
        // 분할점 수집과 노드 병합은 3D 거리 기준이다. 표고가 다른 입체교차는 허용오차를 벗어나므로 분기로 잡히지 않는다.
        var splits = CollectSplitParameters(segments, snapTolerance);
        var edges = SplitSegments(segments, splits, snapTolerance);
        return AssembleGraph(edges, snapTolerance);
    }

    private static List<RawSegment> CollectSegments(IReadOnlyList<PipeAlignment> alignments, double tolerance)
    {
        var segments = new List<RawSegment>();
        for (var i = 0; i < alignments.Count; i++)
        {
            var alignment = alignments[i];
            for (var v = 1; v < alignment.Vertices.Count; v++)
            {
                var start = alignment.Vertices[v - 1];
                var end = alignment.Vertices[v];
                // 허용오차보다 짧은 선분은 중복 정점으로 보고 버린다(길이 0 간선 방지).
                if (start.DistanceTo(end) <= tolerance) continue;
                segments.Add(new RawSegment(i, start, end, alignment.DiameterMm, alignment.PipeKind));
            }
        }
        return segments;
    }

    /// <summary>
    /// 서로 다른 폴리선의 선분 쌍에서 최근접점을 구해 허용오차 안이면 분할 파라미터(0~1)를 기록한다.
    /// 한쪽 최근접점이 선분 끝이면 그쪽은 분할하지 않으므로, 끝점-선분 접촉(T)과 선분-선분 교차(십자)가 같은 계산으로 처리된다.
    /// </summary>
    private static Dictionary<int, List<double>> CollectSplitParameters(List<RawSegment> segments, double tolerance)
    {
        var splits = new Dictionary<int, List<double>>();
        foreach (var (i, j) in CandidatePairs(segments, tolerance))
        {
            var a = segments[i];
            var b = segments[j];
            // 같은 폴리선 내부의 자기 접촉(되돌아오는 선형 등)은 분할 대상에서 제외한다.
            if (a.AlignmentIndex == b.AlignmentIndex) continue;
            if (!TryClosestParameters(a, b, out var s, out var t)) continue;

            var pa = a.PointAt(s);
            var pb = b.PointAt(t);
            if (pa.DistanceTo(pb) > tolerance) continue;

            AddSplit(splits, i, s, a.Length, tolerance);
            AddSplit(splits, j, t, b.Length, tolerance);
        }
        return splits;
    }

    private static void AddSplit(Dictionary<int, List<double>> splits, int index, double parameter, double length, double tolerance)
    {
        // 선분 끝에서 허용오차 안쪽이면 이미 그 끝점이 노드가 되므로 분할하지 않는다.
        if (parameter * length <= tolerance || (1 - parameter) * length <= tolerance) return;
        if (!splits.TryGetValue(index, out var list)) splits[index] = list = new List<double>();
        list.Add(parameter);
    }

    private static List<RawSegment> SplitSegments(List<RawSegment> segments, Dictionary<int, List<double>> splits, double tolerance)
    {
        var result = new List<RawSegment>(segments.Count);
        for (var i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            if (!splits.TryGetValue(i, out var parameters))
            {
                result.Add(segment);
                continue;
            }

            parameters.Sort();
            var previous = 0d;
            foreach (var parameter in parameters)
            {
                // 근접한 분할점은 하나로 합쳐 허용오차보다 짧은 조각이 생기지 않게 한다.
                if ((parameter - previous) * segment.Length <= tolerance) continue;
                result.Add(segment.Sub(previous, parameter));
                previous = parameter;
            }
            if ((1 - previous) * segment.Length > tolerance) result.Add(segment.Sub(previous, 1));
        }
        return result;
    }

    private static PipeNetworkGraph AssembleGraph(List<RawSegment> segments, double tolerance)
    {
        var merger = new NodeMerger(tolerance);
        var edges = new List<NetworkEdge>(segments.Count);
        var incidences = new List<List<NodeIncidence>>();

        foreach (var segment in segments)
        {
            var startId = merger.Resolve(segment.Start, incidences);
            var endId = merger.Resolve(segment.End, incidences);
            if (startId == endId) continue;   // 병합 결과 양 끝이 같은 노드가 된 조각은 버린다.

            var edgeId = edges.Count;
            edges.Add(new NetworkEdge(edgeId, segment.AlignmentIndex, startId, endId, segment.Start, segment.End, segment.DiameterMm, segment.PipeKind));
            incidences[startId].Add(new NodeIncidence(edgeId, true));
            incidences[endId].Add(new NodeIncidence(edgeId, false));
        }

        var nodes = new List<NetworkNode>(merger.Positions.Count);
        for (var i = 0; i < merger.Positions.Count; i++)
            nodes.Add(new NetworkNode(i, merger.Positions[i], incidences[i]));
        return new PipeNetworkGraph(nodes, edges);
    }

    /// <summary>균일 격자로 근접한 선분 쌍만 추린다(전수 비교 O(n²) 회피).</summary>
    private static IEnumerable<(int, int)> CandidatePairs(List<RawSegment> segments, double tolerance)
    {
        if (segments.Count < 2) yield break;

        var cell = Math.Max(tolerance, segments.Average(x => x.Length));
        var grid = new Dictionary<(int, int, int), List<int>>();
        for (var i = 0; i < segments.Count; i++)
            foreach (var key in CellKeys(segments[i], cell, tolerance))
            {
                if (!grid.TryGetValue(key, out var bucket)) grid[key] = bucket = new List<int>();
                bucket.Add(i);
            }

        var seen = new HashSet<(int, int)>();
        foreach (var bucket in grid.Values)
            for (var a = 0; a < bucket.Count; a++)
                for (var b = a + 1; b < bucket.Count; b++)
                {
                    var pair = bucket[a] < bucket[b] ? (bucket[a], bucket[b]) : (bucket[b], bucket[a]);
                    if (seen.Add(pair)) yield return pair;
                }
    }

    private static IEnumerable<(int, int, int)> CellKeys(RawSegment segment, double cell, double tolerance)
    {
        var minX = (int)Math.Floor((Math.Min(segment.Start.X, segment.End.X) - tolerance) / cell);
        var maxX = (int)Math.Floor((Math.Max(segment.Start.X, segment.End.X) + tolerance) / cell);
        var minY = (int)Math.Floor((Math.Min(segment.Start.Y, segment.End.Y) - tolerance) / cell);
        var maxY = (int)Math.Floor((Math.Max(segment.Start.Y, segment.End.Y) + tolerance) / cell);
        var minZ = (int)Math.Floor((Math.Min(segment.Start.Z, segment.End.Z) - tolerance) / cell);
        var maxZ = (int)Math.Floor((Math.Max(segment.Start.Z, segment.End.Z) + tolerance) / cell);
        for (var x = minX; x <= maxX; x++)
            for (var y = minY; y <= maxY; y++)
                for (var z = minZ; z <= maxZ; z++)
                    yield return (x, y, z);
    }

    /// <summary>두 선분의 최근접 파라미터(0~1). 거의 평행한 쌍은 접촉점이 하나로 정해지지 않으므로 제외한다.</summary>
    private static bool TryClosestParameters(RawSegment a, RawSegment b, out double s, out double t)
    {
        s = t = 0;
        var u = a.Direction;
        var v = b.Direction;
        var w = new Vector3D(a.Start.X - b.Start.X, a.Start.Y - b.Start.Y, a.Start.Z - b.Start.Z);
        var uu = Dot(u, u);
        var uv = Dot(u, v);
        var vv = Dot(v, v);
        var uw = Dot(u, w);
        var vw = Dot(v, w);
        var denominator = uu * vv - uv * uv;
        // TODO: 평행 중첩(같은 경로에 폴리선이 겹쳐 그려진 경우)은 데이터 오류로 별도 진단이 필요하다.
        if (Math.Abs(denominator) <= 1e-12) return false;

        s = Math.Clamp((uv * vw - vv * uw) / denominator, 0, 1);
        t = Math.Clamp((uu * vw - uv * uw) / denominator, 0, 1);
        // 한쪽을 끝점으로 클램프한 경우 반대쪽 파라미터를 다시 투영해야 실제 최근접점이 된다.
        t = Math.Clamp((Dot(v, new Vector3D(a.Start.X + u.X * s - b.Start.X, a.Start.Y + u.Y * s - b.Start.Y, a.Start.Z + u.Z * s - b.Start.Z))) / vv, 0, 1);
        s = Math.Clamp((Dot(u, new Vector3D(b.Start.X + v.X * t - a.Start.X, b.Start.Y + v.Y * t - a.Start.Y, b.Start.Z + v.Z * t - a.Start.Z))) / uu, 0, 1);
        return true;
    }

    private static double Dot(Vector3D a, Vector3D b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    /// <summary>격자 해시로 허용오차 안의 점을 같은 노드로 병합한다.</summary>
    private sealed class NodeMerger
    {
        private readonly double _tolerance;
        private readonly Dictionary<(int, int, int), List<int>> _grid = new();

        public NodeMerger(double tolerance) => _tolerance = tolerance;

        public List<Point3D> Positions { get; } = new();

        public int Resolve(Point3D point, List<List<NodeIncidence>> incidences)
        {
            var key = Key(point);
            for (var dx = -1; dx <= 1; dx++)
                for (var dy = -1; dy <= 1; dy++)
                    for (var dz = -1; dz <= 1; dz++)
                    {
                        if (!_grid.TryGetValue((key.Item1 + dx, key.Item2 + dy, key.Item3 + dz), out var bucket)) continue;
                        foreach (var id in bucket)
                            if (Positions[id].DistanceTo(point) <= _tolerance) return id;
                    }

            // 병합 대상이 없으면 새 노드. 대표 좌표는 먼저 등장한 점으로 고정한다(평균을 쓰면 순서에 따라 좌표가 흔들린다).
            var newId = Positions.Count;
            Positions.Add(point);
            incidences.Add(new List<NodeIncidence>());
            if (!_grid.TryGetValue(key, out var list)) _grid[key] = list = new List<int>();
            list.Add(newId);
            return newId;
        }

        private (int, int, int) Key(Point3D p) =>
            ((int)Math.Floor(p.X / _tolerance), (int)Math.Floor(p.Y / _tolerance), (int)Math.Floor(p.Z / _tolerance));
    }

    private sealed record RawSegment(int AlignmentIndex, Point3D Start, Point3D End, double DiameterMm, string PipeKind)
    {
        public double Length => Start.DistanceTo(End);

        /// <summary>정규화하지 않은 방향벡터(파라미터 0~1 기준).</summary>
        public Vector3D Direction => new(End.X - Start.X, End.Y - Start.Y, End.Z - Start.Z);

        public Point3D PointAt(double parameter) => new(
            Start.X + (End.X - Start.X) * parameter,
            Start.Y + (End.Y - Start.Y) * parameter,
            Start.Z + (End.Z - Start.Z) * parameter);

        public RawSegment Sub(double from, double to) => this with { Start = PointAt(from), End = PointAt(to) };
    }
}
