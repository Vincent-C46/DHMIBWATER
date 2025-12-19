namespace DHBIMWATER.Core.Domain.ValueObjects.Dimensions;

/// <summary>
/// 두께 값 객체 (최소/최대 검증 포함)
/// </summary>
public sealed class Thickness : IEquatable<Thickness>
{
    private const double MinThickness = 100.0; // 100mm
    private const double MaxThickness = 2000.0; // 2000mm

    public Length Value { get; }

    private Thickness(Length value)
    {
        if (value.Millimeters < MinThickness)
            throw new ArgumentException($"두께는 최소 {MinThickness}mm 이상이어야 합니다.", nameof(value));

        if (value.Millimeters > MaxThickness)
            throw new ArgumentException($"두께는 최대 {MaxThickness}mm 이하여야 합니다.", nameof(value));

        Value = value;
    }

    public static Thickness FromMillimeters(double millimeters)
        => new(Length.FromMillimeters(millimeters));

    public static Thickness FromMeters(double meters)
        => new(Length.FromMeters(meters));

    public bool Equals(Thickness? other)
    {
        if (other is null) return false;
        return Value.Equals(other.Value);
    }

    public override bool Equals(object? obj) => obj is Thickness other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString();

    public static bool operator ==(Thickness? left, Thickness? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(Thickness? left, Thickness? right) => !(left == right);
}
