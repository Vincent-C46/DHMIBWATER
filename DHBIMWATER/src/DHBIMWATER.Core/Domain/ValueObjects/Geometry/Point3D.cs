namespace DHBIMWATER.Core.Domain.ValueObjects.Geometry;

/// <summary>
/// 3D 공간 좌표
/// </summary>
public sealed class Point3D : IEquatable<Point3D>
{
    public double X { get; }
    public double Y { get; }
    public double Z { get; }

    public Point3D(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static Point3D Origin => new(0, 0, 0);

    /// <summary>
    /// 두 점 사이의 거리
    /// </summary>
    public double DistanceTo(Point3D other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        var dz = Z - other.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    public bool Equals(Point3D? other)
    {
        if (other is null) return false;
        return Math.Abs(X - other.X) < 0.001 &&
               Math.Abs(Y - other.Y) < 0.001 &&
               Math.Abs(Z - other.Z) < 0.001;
    }

    public override bool Equals(object? obj) => obj is Point3D other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

    public override string ToString() => $"({X:F3}, {Y:F3}, {Z:F3})";

    public static bool operator ==(Point3D? left, Point3D? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(Point3D? left, Point3D? right) => !(left == right);

    public static Point3D operator +(Point3D left, Point3D right)
        => new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

    public static Point3D operator -(Point3D left, Point3D right)
        => new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
}
