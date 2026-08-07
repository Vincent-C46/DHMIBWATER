using DHBIMWATER.Core.Geometry;

namespace DHBIMWATER.Core.Gis;

/// <param name="Start">P1 — 절점에서 한쪽 직관 방향으로 t 떨어진 관 끝(Connector 지점).</param>
/// <param name="ArcMid">P2 — 실제 호의 중점. 3점 가변 패밀리의 두 번째 점이다.</param>
/// <param name="End">P3 — 반대쪽 관 끝(Connector 지점).</param>
/// <param name="ExternalMm">E = R·(sec(θ/2) − 1). 절점에서 호 중점까지의 거리.</param>
public sealed record BendArcPoints(Point3D Start, Point3D ArcMid, Point3D End, double ExternalMm);

/// <summary>
/// 절점 편각과 곡관 치수(R, t)로 3점 가변 곡관 패밀리의 배치점 P1/P2/P3을 계산한다.
/// 근거와 기호는 docs/20_관로네트워크_분기곡관_지시서.md §10-3 참조. Revit 의존 없는 순수 계산이다.
/// </summary>
/// <remarks>
/// P1/P3은 호의 시작·끝점이 아니다. 주철관 곡관은 호 양끝에 직관부가 붙어 있어 관 끝(t)이 접점(T)보다 바깥에 있다.
/// P2만 실제 호 위의 점이다.
/// </remarks>
public static class BendArcGeometry
{
    private const double DegenerateEpsilon = 1e-9;

    /// <param name="node">절점 V. 좌표 단위는 원본 GIS 좌표(m).</param>
    /// <param name="dirA">V에서 한쪽 직관으로 나가는 단위벡터(<see cref="PipeNetworkGraph.DirectionFrom"/>).</param>
    /// <param name="dirB">V에서 반대쪽 직관으로 나가는 단위벡터.</param>
    /// <param name="angleDeg">곡관 각도 θ(=편각). 두 방향의 사잇각은 180−θ다.</param>
    /// <param name="radiusMm">R — 중심선 호의 곡률반경(mm).</param>
    /// <param name="layingLengthMm">t — 절점에서 관 끝까지의 거리(mm). 양방향 동일하다.</param>
    /// <param name="mmToCoordinate">mm를 좌표 단위로 바꾸는 배율. 기본 0.001(mm→m).</param>
    public static BendArcPoints Compute(
        Point3D node,
        Vector3D dirA,
        Vector3D dirB,
        double angleDeg,
        double radiusMm,
        double layingLengthMm,
        double mmToCoordinate = 0.001)
        => Compute(node, dirA, dirB, angleDeg, radiusMm, layingLengthMm, layingLengthMm, mmToCoordinate);

    /// <summary>
    /// 양쪽 관 끝 거리가 다른 곡관(B형 — 한쪽에 직관부 s가 더 붙는다)용.
    /// </summary>
    /// <remarks>
    /// 호 자체는 비대칭의 영향을 받지 않는다. 호는 R과 θ만으로 결정되고 접점은 절점에서 양쪽 T로 대칭이라,
    /// 달라지는 것은 접점 바깥 직관부 길이(= 관 끝 P1/P3의 위치)뿐이다. P2 계산식은 그대로다.
    /// </remarks>
    /// <param name="layingLengthAMm">dirA 방향 관 끝까지의 거리(mm).</param>
    /// <param name="layingLengthBMm">dirB 방향 관 끝까지의 거리(mm).</param>
    public static BendArcPoints Compute(
        Point3D node,
        Vector3D dirA,
        Vector3D dirB,
        double angleDeg,
        double radiusMm,
        double layingLengthAMm,
        double layingLengthBMm,
        double mmToCoordinate = 0.001)
    {
        // V에서 호 쪽을 향하는 내각 이등분선. 두 방향이 정반대(편각 0)면 정의되지 않는다.
        var sum = new Vector3D(dirA.X + dirB.X, dirA.Y + dirB.Y, dirA.Z + dirB.Z);
        if (sum.Length <= DegenerateEpsilon)
            throw new ArgumentException("편각이 0이라 곡관 이등분선을 정의할 수 없다.", nameof(angleDeg));
        var bisector = sum.Normalize();

        var externalMm = ExternalDistance(radiusMm, angleDeg);
        var e = externalMm * mmToCoordinate;

        return new BendArcPoints(
            Offset(node, dirA, layingLengthAMm * mmToCoordinate),
            Offset(node, bisector, e),
            Offset(node, dirB, layingLengthBMm * mmToCoordinate),
            externalMm);
    }

    /// <summary>외거 E = R·(sec(θ/2) − 1). 절점에서 호 중점까지의 거리(mm).</summary>
    public static double ExternalDistance(double radiusMm, double angleDeg)
        => radiusMm * (1d / Math.Cos(angleDeg * Math.PI / 360d) - 1d);

    private static Point3D Offset(Point3D origin, Vector3D direction, double distance)
        => new(origin.X + direction.X * distance, origin.Y + direction.Y * distance, origin.Z + direction.Z * distance);
}
