using DHBIMWATER.Core.Geometry;

namespace DHBIMWATER.Core.Gis;

public sealed record AlignmentSamplePoint(Point3D Position, Vector3D Tangent, double DistanceFromStart);
public sealed record AlignmentSampleSegment(Point3D Start, Point3D End, double DistanceFromStart);

/// <summary>폴리라인의 호 길이를 기준으로 균일 간격 지점을 샘플링한다. 좌표와 간격은 같은 단위를 사용한다.</summary>
public static class AlignmentIntervalSampler
{
    public static IReadOnlyList<AlignmentSamplePoint> SamplePoints(IReadOnlyList<Point3D> vertices, double interval)
    {
        ValidateInterval(interval);
        var parts = BuildParts(vertices);
        if (parts.Count == 0) return Array.Empty<AlignmentSamplePoint>();

        var total = parts[^1].EndDistance;
        var result = new List<AlignmentSamplePoint>();
        for (var distance = 0d; distance <= total + 1e-9; distance += interval)
        {
            if (distance > total + 1e-9) break;
            var part = parts.First(x => distance <= x.EndDistance + 1e-9);
            var local = Math.Clamp((distance - part.StartDistance) / part.Length, 0d, 1d);
            result.Add(new AlignmentSamplePoint(
                new Point3D(part.Start.X + (part.End.X - part.Start.X) * local, part.Start.Y + (part.End.Y - part.Start.Y) * local, part.Start.Z + (part.End.Z - part.Start.Z) * local),
                new Vector3D((part.End.X - part.Start.X) / part.Length, (part.End.Y - part.Start.Y) / part.Length, (part.End.Z - part.Start.Z) / part.Length),
                Math.Min(distance, total)));
        }
        return result;
    }

    public static IReadOnlyList<AlignmentSampleSegment> SampleSegments(IReadOnlyList<Point3D> vertices, double interval)
    {
        ValidateInterval(interval);
        var parts = BuildParts(vertices);
        if (parts.Count == 0) return Array.Empty<AlignmentSampleSegment>();

        var total = parts[^1].EndDistance;
        var boundaries = SamplePoints(vertices, interval).Select(x => (x.Position, x.DistanceFromStart)).ToList();
        if (boundaries[^1].Item2 < total - 1e-9) boundaries.Add((PointAt(parts, total), total));
        return boundaries.Zip(boundaries.Skip(1), (a, b) => new AlignmentSampleSegment(a.Position, b.Position, a.Item2)).ToList();
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
