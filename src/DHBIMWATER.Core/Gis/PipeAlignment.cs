using DHBIMWATER.Core.Geometry;

namespace DHBIMWATER.Core.Gis;

/// <summary>GIS 원본 좌표(m)로 구성된 관로 중심선 파트이다.</summary>
public sealed record PipeAlignment(
    IReadOnlyList<Point3D> Vertices,
    string PipeKind,
    double DiameterMm,
    string SourceFile,
    string RecordNumber,
    IReadOnlyDictionary<string, string> Attributes);

/// <summary>원본 데이터의 Z가 관의 어느 위치를 가리키는지. 배치 시 전부 중심선 Z로 환산한다.</summary>
public enum ZDatum
{
    /// <summary>관 한가운데. 보정 없음.</summary>
    Centerline,
    /// <summary>관 내부 바닥(관저고). 국내 상수도 SHP의 기본값.</summary>
    Invert,
    /// <summary>관 내부 천장.</summary>
    Crown,
    /// <summary>관 재료 포함 가장 위.</summary>
    OutsideTop,
    /// <summary>관 재료 포함 가장 아래.</summary>
    OutsideBottom
}
public enum ZSource { GeometryZ, AttributeField, None2D }
