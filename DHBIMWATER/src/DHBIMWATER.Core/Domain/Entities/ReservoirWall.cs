using DHBIMWATER.Core.Domain.Enums;
using DHBIMWATER.Core.Domain.ValueObjects.Dimensions;
using DHBIMWATER.Core.Domain.ValueObjects.Geometry;

namespace DHBIMWATER.Core.Domain.Entities;

/// <summary>
/// 벽체 엔티티
/// </summary>
public class ReservoirWall
{
    public Guid Id { get; private set; }
    public WallType WallType { get; private set; }
    public Thickness Thickness { get; private set; }
    public Length Height { get; private set; }
    public Point3D StartPoint { get; private set; }
    public Point3D EndPoint { get; private set; }

    /// <summary>
    /// 개구부 목록 (맨홀, 환기구 등)
    /// </summary>
    public List<ReservoirOpening> Openings { get; private set; }

    private ReservoirWall()
    {
        Openings = new List<ReservoirOpening>();
    }

    public static ReservoirWall Create(
        WallType wallType,
        Thickness thickness,
        Length height,
        Point3D startPoint,
        Point3D endPoint)
    {
        return new ReservoirWall
        {
            Id = Guid.NewGuid(),
            WallType = wallType,
            Thickness = thickness,
            Height = height,
            StartPoint = startPoint,
            EndPoint = endPoint
        };
    }

    /// <summary>
    /// 벽체 길이 계산
    /// </summary>
    public double GetLength()
    {
        return StartPoint.DistanceTo(EndPoint);
    }

    public void AddOpening(ReservoirOpening opening)
    {
        ArgumentNullException.ThrowIfNull(opening);
        Openings.Add(opening);
    }
}
