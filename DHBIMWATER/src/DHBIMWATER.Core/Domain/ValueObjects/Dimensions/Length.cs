namespace DHBIMWATER.Core.Domain.ValueObjects.Dimensions;

/// <summary>
/// 길이 값 객체 (mm 기준)
/// </summary>
public sealed class Length : IEquatable<Length>
{
    /// <summary>
    /// mm 단위 값
    /// </summary>
    public double Millimeters { get; }

    /// <summary>
    /// m 단위 값
    /// </summary>
    public double Meters => Millimeters / 1000.0;

    /// <summary>
    /// feet 단위 값
    /// </summary>
    public double Feet => Millimeters / 304.8;

    private Length(double millimeters)
    {
        if (millimeters < 0)
            throw new ArgumentException("길이는 0 이상이어야 합니다.", nameof(millimeters));

        Millimeters = millimeters;
    }

    /// <summary>
    /// mm 단위로 생성
    /// </summary>
    public static Length FromMillimeters(double millimeters) => new(millimeters);

    /// <summary>
    /// m 단위로 생성
    /// </summary>
    public static Length FromMeters(double meters) => new(meters * 1000.0);

    /// <summary>
    /// feet 단위로 생성
    /// </summary>
    public static Length FromFeet(double feet) => new(feet * 304.8);

    public bool Equals(Length? other)
    {
        if (other is null) return false;
        return Math.Abs(Millimeters - other.Millimeters) < 0.001;
    }

    public override bool Equals(object? obj) => obj is Length other && Equals(other);

    public override int GetHashCode() => Millimeters.GetHashCode();

    public override string ToString() => $"{Millimeters:F1}mm";

    public static bool operator ==(Length? left, Length? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(Length? left, Length? right) => !(left == right);

    public static Length operator +(Length left, Length right)
        => FromMillimeters(left.Millimeters + right.Millimeters);

    public static Length operator -(Length left, Length right)
        => FromMillimeters(left.Millimeters - right.Millimeters);

    public static Length operator *(Length left, double scalar)
        => FromMillimeters(left.Millimeters * scalar);

    public static Length operator /(Length left, double scalar)
        => FromMillimeters(left.Millimeters / scalar);
}
