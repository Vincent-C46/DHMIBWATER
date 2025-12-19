using DHBIMWATER.Core.Domain.Enums;
using DHBIMWATER.Core.Domain.ValueObjects.Dimensions;

namespace DHBIMWATER.Core.Domain.Entities;

/// <summary>
/// 배수지 루트 엔티티
/// </summary>
public class DistributionReservoir
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public ReservoirType Type { get; private set; }

    /// <summary>
    /// 내부 길이 (L)
    /// </summary>
    public Length InternalLength { get; private set; }

    /// <summary>
    /// 내부 폭 (W)
    /// </summary>
    public Length InternalWidth { get; private set; }

    /// <summary>
    /// 내부 높이 (H)
    /// </summary>
    public Length InternalHeight { get; private set; }

    /// <summary>
    /// 벽체 컬렉션
    /// </summary>
    public List<ReservoirWall> Walls { get; private set; }

    /// <summary>
    /// 슬래브 컬렉션
    /// </summary>
    public List<ReservoirSlab> Slabs { get; private set; }

    /// <summary>
    /// 기둥 컬렉션
    /// </summary>
    public List<ReservoirColumn> Columns { get; private set; }

    /// <summary>
    /// 보 컬렉션
    /// </summary>
    public List<ReservoirBeam> Beams { get; private set; }

    /// <summary>
    /// 배관 컬렉션
    /// </summary>
    public List<ReservoirPipe> Pipes { get; private set; }

    private DistributionReservoir()
    {
        Walls = new List<ReservoirWall>();
        Slabs = new List<ReservoirSlab>();
        Columns = new List<ReservoirColumn>();
        Beams = new List<ReservoirBeam>();
        Pipes = new List<ReservoirPipe>();
    }

    public static DistributionReservoir Create(
        string name,
        ReservoirType type,
        Length internalLength,
        Length internalWidth,
        Length internalHeight)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("배수지 이름은 필수입니다.", nameof(name));

        return new DistributionReservoir
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            InternalLength = internalLength,
            InternalWidth = internalWidth,
            InternalHeight = internalHeight
        };
    }

    /// <summary>
    /// 유효 용량 계산 (m³)
    /// </summary>
    public double CalculateCapacity()
    {
        var volumeInMm3 = InternalLength.Millimeters *
                         InternalWidth.Millimeters *
                         InternalHeight.Millimeters;

        return volumeInMm3 / 1_000_000_000.0; // mm³ to m³
    }

    public void AddWall(ReservoirWall wall)
    {
        ArgumentNullException.ThrowIfNull(wall);
        Walls.Add(wall);
    }

    public void AddSlab(ReservoirSlab slab)
    {
        ArgumentNullException.ThrowIfNull(slab);
        Slabs.Add(slab);
    }

    public void AddColumn(ReservoirColumn column)
    {
        ArgumentNullException.ThrowIfNull(column);
        Columns.Add(column);
    }

    public void AddBeam(ReservoirBeam beam)
    {
        ArgumentNullException.ThrowIfNull(beam);
        Beams.Add(beam);
    }

    public void AddPipe(ReservoirPipe pipe)
    {
        ArgumentNullException.ThrowIfNull(pipe);
        Pipes.Add(pipe);
    }
}
