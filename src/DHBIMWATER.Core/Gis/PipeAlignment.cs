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

public enum ZDatum { AsIs, Center, Invert, Crown }
public enum ZSource { GeometryZ, AttributeField, None2D }
