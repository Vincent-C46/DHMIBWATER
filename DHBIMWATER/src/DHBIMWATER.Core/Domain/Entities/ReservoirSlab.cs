using DHBIMWATER.Core.Domain.Enums;
using DHBIMWATER.Core.Domain.ValueObjects.Dimensions;
using DHBIMWATER.Core.Domain.ValueObjects.Geometry;

namespace DHBIMWATER.Core.Domain.Entities;

/// <summary>
/// 슬래브 엔티티
/// </summary>
public class ReservoirSlab
{
    public Guid Id { get; private set; }
    public SlabType SlabType { get; private set; }
    public Thickness Thickness { get; private set; }
    public Elevation Elevation { get; private set; }

    /// <summary>
    /// 경계 점 목록
    /// </summary>
    public List<Point3D> BoundaryPoints { get; private set; }

    /// <summary>
    /// 개구부 목록
    /// </summary>
    public List<ReservoirOpening> Openings { get; private set; }

    private ReservoirSlab()
    {
        BoundaryPoints = new List<Point3D>();
        Openings = new List<ReservoirOpening>();
    }

    public static ReservoirSlab Create(
        SlabType slabType,
        Thickness thickness,
        Elevation elevation,
        List<Point3D> boundaryPoints)
    {
        if (boundaryPoints == null || boundaryPoints.Count < 3)
            throw new ArgumentException("슬래브 경계는 최소 3개 이상의 점이 필요합니다.", nameof(boundaryPoints));

        return new ReservoirSlab
        {
            Id = Guid.NewGuid(),
            SlabType = slabType,
            Thickness = thickness,
            Elevation = elevation,
            BoundaryPoints = new List<Point3D>(boundaryPoints)
        };
    }

    public void AddOpening(ReservoirOpening opening)
    {
        ArgumentNullException.ThrowIfNull(opening);
        Openings.Add(opening);
    }
}
