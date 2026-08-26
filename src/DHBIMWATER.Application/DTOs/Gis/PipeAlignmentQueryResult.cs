using DHBIMWATER.Core.Geometry;

namespace DHBIMWATER.Application.DTOs.Gis;

/// <summary>
/// DirectShape에서 되읽은 관로 선형. Vertices는 Revit 내부원점 기준 상대좌표(mm)이며,
/// 원본 SHP 절대좌표(m)로는 복원하지 않는다 — 기준점이 프로젝트에 영속 저장되지 않기 때문.
/// Phase 2 배치(패밀리/파이프)는 상대좌표만으로 충분하다는 전제로 설계했다.
/// </summary>
public sealed record PipeAlignmentQueryResult(
    int ElementId,
    IReadOnlyList<Point3D> Vertices,
    string PipeKind,
    double DiameterMm);
