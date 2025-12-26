namespace DHBIMWATER.Shared.Helpers;

/// <summary>
/// 단위 변환 헬퍼
/// </summary>
public static class UnitConverter
{
    // mm <-> m
    public const double MillimetersPerMeter = 1000.0;

    // mm <-> feet
    public const double MillimetersPerFoot = 304.8;

    // Revit Internal Units (feet)
    public const double FeetPerMeter = 3.28084;

    public static double MillimetersToMeters(double millimeters)
        => millimeters / MillimetersPerMeter;

    public static double MetersToMillimeters(double meters)
        => meters * MillimetersPerMeter;

    public static double MillimetersToFeet(double millimeters)
        => millimeters / MillimetersPerFoot;

    public static double FeetToMillimeters(double feet)
        => feet * MillimetersPerFoot;

    public static double MetersToFeet(double meters)
        => meters * FeetPerMeter;

    public static double FeetToMeters(double feet)
        => feet / FeetPerMeter;
}
