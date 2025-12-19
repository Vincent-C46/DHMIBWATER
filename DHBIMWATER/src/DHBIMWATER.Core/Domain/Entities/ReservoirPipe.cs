using DHBIMWATER.Core.Domain.Enums;
using DHBIMWATER.Core.Domain.ValueObjects.Dimensions;
using DHBIMWATER.Core.Domain.ValueObjects.Geometry;

namespace DHBIMWATER.Core.Domain.Entities;

/// <summary>
/// 배관 엔티티
/// </summary>
public class ReservoirPipe
{
    public Guid Id { get; private set; }
    public PipeType PipeType { get; private set; }
    public Point3D Location { get; private set; }
    public Length Diameter { get; private set; }

    /// <summary>
    /// 관저고 (Invert Level)
    /// </summary>
    public Elevation InvertLevel { get; private set; }

    private ReservoirPipe() { }

    public static ReservoirPipe Create(
        PipeType pipeType,
        Point3D location,
        Length diameter,
        Elevation invertLevel)
    {
        return new ReservoirPipe
        {
            Id = Guid.NewGuid(),
            PipeType = pipeType,
            Location = location,
            Diameter = diameter,
            InvertLevel = invertLevel
        };
    }
}
