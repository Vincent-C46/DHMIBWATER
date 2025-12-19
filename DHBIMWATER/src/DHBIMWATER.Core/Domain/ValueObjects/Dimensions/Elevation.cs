namespace DHBIMWATER.Core.Domain.ValueObjects.Dimensions;

/// <summary>
/// 표고 값 객체
/// </summary>
public sealed class Elevation : IEquatable<Elevation>
{
    public Length Value { get; }

    private Elevation(Length value)
    {
        Value = value;
    }

    public static Elevation FromMillimeters(double millimeters)
        => new(Length.FromMillimeters(millimeters));

    public static Elevation FromMeters(double meters)
        => new(Length.FromMeters(meters));

    /// <summary>
    /// EL+123.45 형식으로 표현
    /// </summary>
    public string ToElevationString()
    {
        var meters = Value.Meters;
        var sign = meters >= 0 ? "+" : "-";
        return $"EL{sign}{Math.Abs(meters):F2}";
    }

    public bool Equals(Elevation? other)
    {
        if (other is null) return false;
        return Value.Equals(other.Value);
    }

    public override bool Equals(object? obj) => obj is Elevation other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => ToElevationString();

    public static bool operator ==(Elevation? left, Elevation? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(Elevation? left, Elevation? right) => !(left == right);
}
