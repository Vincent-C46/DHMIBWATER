using DHBIMWATER.Core.Geometry;

namespace DHBIMWATER.Core.Gis;

public sealed record AlignmentSamplePoint(Point3D Position, Vector3D Tangent, double DistanceFromStart);
public sealed record AlignmentSampleSegment(Point3D Start, Point3D End, double DistanceFromStart);

/// <summary>폴리라인의 호 길이를 기준으로 균일 간격 지점을 샘플링한다. 좌표와 간격은 같은 단위를 사용한다.</summary>
public static class AlignmentIntervalSampler
{
    private const double Tolerance = 1e-9;

    public static IReadOnlyList<AlignmentSamplePoint> SamplePoints(IReadOnlyList<Point3D> vertices, double interval)
    {
        ValidateInterval(interval);
        var parts = BuildParts(vertices);
        if (parts.Count == 0) return Array.Empty<AlignmentSamplePoint>();

        var total = parts[^1].EndDistance;
        var result = new List<AlignmentSamplePoint>();
        for (var i = 0; ; i++)
        {
            var distance = i * interval;              // 누산 대신 인덱스 곱 — 장거리 선형에서 오차 누적 방지
            if (distance > total + Tolerance) break;
            var part = parts.First(x => distance <= x.EndDistance + 1e-9);
            var local = Math.Clamp((distance - part.StartDistance) / part.Length, 0d, 1d);
            result.Add(new AlignmentSamplePoint(
                new Point3D(part.Start.X + (part.End.X - part.Start.X) * local, part.Start.Y + (part.End.Y - part.Start.Y) * local, part.Start.Z + (part.End.Z - part.Start.Z) * local),
                new Vector3D((part.End.X - part.Start.X) / part.Length, (part.End.Y - part.Start.Y) / part.Length, (part.End.Z - part.Start.Z) / part.Length),
                Math.Min(distance, total)));
        }
        return result;
    }

    // TODO: 절점에 곡관이 들어가면 직관 구간은 절점이 아니라 곡관 몸통 끝에서 시작해야 한다.
    //       (절점별 곡관 연장을 받아 파트 양끝에서 차감한 뒤 그 지점부터 interval 분할)
    //       곡관 카탈로그(직경×각도별 연장)와 곡관 판정이 준비되는 배치 단계에서 처리한다.
    public static IReadOnlyList<AlignmentSampleSegment> SampleSegments(IReadOnlyList<Point3D> vertices, double interval)
    {
        ValidateInterval(interval);
        var parts = BuildParts(vertices);
        if (parts.Count == 0) return Array.Empty<AlignmentSampleSegment>();

        var total = parts[^1].EndDistance;
        // 분절 경계 = 절점 ∪ 각 절점부터 다시 잰 interval 배수 ∪ 시작/끝점.
        // 간격은 폴리라인 전체 누적거리가 아니라 절점마다 리셋한다. 전체 누적거리로 재면 앞 구간의
        // 잔여 길이가 다음 구간으로 전파돼, 절점도 없는 위치에서 임의 길이로 끊긴다.
        // 절점을 경계에 포함하지 않으면 코너를 가로지르는 직선 현이 생겨 도면 형상과 달라진다.
        var distances = new List<double> { 0d, total };
        foreach (var part in parts)
        {
            for (var i = 1; i * interval < part.Length; i++) distances.Add(part.StartDistance + i * interval);
            distances.Add(part.EndDistance);
        }

        distances.Sort();
        var boundaries = new List<double>(distances.Count);
        foreach (var d in distances)
        {
            var clamped = Math.Clamp(d, 0d, total);
            // 절점 거리와 interval 배수가 겹칠 때 길이 0 세그먼트가 생기지 않도록 tolerance 기준으로 중복 제거한다.
            if (boundaries.Count == 0 || clamped - boundaries[^1] > Tolerance) boundaries.Add(clamped);
        }
        if (boundaries.Count < 2) return Array.Empty<AlignmentSampleSegment>();

        var result = new List<AlignmentSampleSegment>(boundaries.Count - 1);
        for (var i = 1; i < boundaries.Count; i++)
            result.Add(new AlignmentSampleSegment(PointAt(parts, boundaries[i - 1]), PointAt(parts, boundaries[i]), boundaries[i - 1]));
        return result;
    }

    private static Point3D PointAt(IReadOnlyList<Part> parts, double distance)
    {
        var part = parts.First(x => distance <= x.EndDistance + 1e-9);
        var local = Math.Clamp((distance - part.StartDistance) / part.Length, 0d, 1d);
        return new Point3D(part.Start.X + (part.End.X - part.Start.X) * local, part.Start.Y + (part.End.Y - part.Start.Y) * local, part.Start.Z + (part.End.Z - part.Start.Z) * local);
    }

    private static List<Part> BuildParts(IReadOnlyList<Point3D> vertices)
    {
        var parts = new List<Part>();
        var distance = 0d;
        for (var i = 1; i < vertices.Count; i++)
        {
            var length = vertices[i - 1].DistanceTo(vertices[i]);
            if (length <= 1e-9) continue;
            parts.Add(new Part(vertices[i - 1], vertices[i], distance, distance + length, length));
            distance += length;
        }
        return parts;
    }

    private static void ValidateInterval(double interval)
    {
        if (interval <= 0) throw new ArgumentOutOfRangeException(nameof(interval), "간격은 0보다 커야 합니다.");
    }

    private sealed record Part(Point3D Start, Point3D End, double StartDistance, double EndDistance, double Length);
}
