using DHBIMWATER.Core.Domain.ValueObjects.Dimensions;
using DHBIMWATER.Core.Domain.ValueObjects.Geometry;

namespace DHBIMWATER.Core.Domain.Entities;

/// <summary>
/// 보 엔티티
/// </summary>
public class ReservoirBeam
{
    public Guid Id { get; private set; }
    public Point3D StartPoint { get; private set; }
    public Point3D EndPoint { get; private set; }
    public Length Width { get; private set; }
    public Length Height { get; private set; }
    public string FamilyTypeName { get; private set; }

    private ReservoirBeam() { }

    public static ReservoirBeam Create(
        Point3D startPoint,
        Point3D endPoint,
        Length width,
        Length height,
        string familyTypeName = "기본보")
    {
        if (string.IsNullOrWhiteSpace(familyTypeName))
            throw new ArgumentException("패밀리 타입 이름은 필수입니다.", nameof(familyTypeName));

        return new ReservoirBeam
        {
            Id = Guid.NewGuid(),
            StartPoint = startPoint,
            EndPoint = endPoint,
            Width = width,
            Height = height,
            FamilyTypeName = familyTypeName
        };
    }

    /// <summary>
    /// 보 길이 계산
    /// </summary>
    public double GetLength()
    {
        return StartPoint.DistanceTo(EndPoint);
    }
}
