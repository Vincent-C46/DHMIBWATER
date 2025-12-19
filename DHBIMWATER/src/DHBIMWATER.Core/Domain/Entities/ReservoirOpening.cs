using DHBIMWATER.Core.Domain.Enums;
using DHBIMWATER.Core.Domain.ValueObjects.Dimensions;
using DHBIMWATER.Core.Domain.ValueObjects.Geometry;

namespace DHBIMWATER.Core.Domain.Entities;

/// <summary>
/// 개구부 종류
/// </summary>
public enum OpeningType
{
    /// <summary>
    /// 맨홀
    /// </summary>
    Manhole,

    /// <summary>
    /// 환기구
    /// </summary>
    Ventilation,

    /// <summary>
    /// 사다리
    /// </summary>
    Ladder,

    /// <summary>
    /// 출입구
    /// </summary>
    Entrance
}

/// <summary>
/// 개구부 엔티티
/// </summary>
public class ReservoirOpening
{
    public Guid Id { get; private set; }
    public OpeningType OpeningType { get; private set; }
    public Point3D Location { get; private set; }
    public Length Width { get; private set; }
    public Length Height { get; private set; }

    private ReservoirOpening() { }

    public static ReservoirOpening Create(
        OpeningType openingType,
        Point3D location,
        Length width,
        Length height)
    {
        return new ReservoirOpening
        {
            Id = Guid.NewGuid(),
            OpeningType = openingType,
            Location = location,
            Width = width,
            Height = height
        };
    }
}
