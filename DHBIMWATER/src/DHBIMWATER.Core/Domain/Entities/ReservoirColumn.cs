using DHBIMWATER.Core.Domain.ValueObjects.Dimensions;
using DHBIMWATER.Core.Domain.ValueObjects.Geometry;

namespace DHBIMWATER.Core.Domain.Entities;

/// <summary>
/// 기둥 엔티티
/// </summary>
public class ReservoirColumn
{
    public Guid Id { get; private set; }
    public Point3D Location { get; private set; }
    public Length Width { get; private set; }
    public Length Depth { get; private set; }
    public Length Height { get; private set; }
    public string FamilyTypeName { get; private set; }

    private ReservoirColumn() { }

    public static ReservoirColumn Create(
        Point3D location,
        Length width,
        Length depth,
        Length height,
        string familyTypeName = "기본기둥")
    {
        if (string.IsNullOrWhiteSpace(familyTypeName))
            throw new ArgumentException("패밀리 타입 이름은 필수입니다.", nameof(familyTypeName));

        return new ReservoirColumn
        {
            Id = Guid.NewGuid(),
            Location = location,
            Width = width,
            Depth = depth,
            Height = height,
            FamilyTypeName = familyTypeName
        };
    }
}
