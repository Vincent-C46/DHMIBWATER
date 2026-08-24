using DHBIMWATER.Core.Geometry;

namespace DHBIMWATER.Core.Gis;

/// <param name="Start">P1 — 곡관 시작(관 끝). 절점에서 한쪽 직관 방향으로 t 떨어진 지점.</param>
/// <param name="ArcStart">P2 — 호의 시작(접점 A). 절점에서 같은 방향으로 T 떨어진 지점.</param>
/// <param name="ArcMid">P3 — 호의 중간점. 5점 가변 패밀리의 세 번째 점이다.</param>
/// <param name="ArcEnd">P4 — 호의 끝(접점 B). 절점에서 반대 방향으로 T 떨어진 지점.</param>
/// <param name="End">P5 — 곡관의 끝(관 끝). 절점에서 반대 방향으로 t 떨어진 지점.</param>
/// <param name="ExternalMm">절점에서 호 중점(P3)까지의 거리. 실제 편각과 표준 곡관각이 같으면 E = R·(sec(θ/2) − 1).</param>
/// <param name="TangentMm">T = R·tan(θ/2). 절점에서 접점(P2/P4)까지의 거리.</param>
public sealed record BendArcPoints(Point3D Start, Point3D ArcStart, Point3D ArcMid, Point3D ArcEnd, Point3D End, double ExternalMm, double TangentMm);

/// <summary>
/// 절점 편각과 곡관 치수(R, t)로 5점 가변 곡관 패밀리의 배치점 P1~P5를 계산한다.
/// 근거와 기호는 docs/20_관로네트워크_분기곡관_지시서.md §10-3 참조. Revit 의존 없는 순수 계산이다.
/// </summary>
/// <remarks>
/// P1/P5는 호의 시작·끝점이 아니다. 주철관 곡관은 호 양끝에 직관부가 붙어 있어 관 끝(t)이 접점(T)보다 바깥에 있다.
/// P2/P4가 실제 접점이고, P3만 호 중앙 위의 점이다(2026-08-11 5점 체계로 확장 — 이전 3점 체계의 P2가 P3이 됐다).
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
    /// <remarks>
    /// mmToCoordinate를 선택 인자로 두면 아래 7-인자 오버로드(양쪽 다리 길이가 다른 버전)와 인자 개수가 겹쳐
    /// 호출부가 의도와 다른 오버로드로 바인딩될 수 있다(2026-08-20 실측 회귀 — 곡관 좌표가 470배 커지는 버그의 원인이었다).
    /// 그래서 이 오버로드는 선택 인자를 두지 않는다.
    /// </remarks>
    public static BendArcPoints Compute(
        Point3D node,
        Vector3D dirA,
        Vector3D dirB,
        double angleDeg,
        double radiusMm,
        double layingLengthMm)
        => ComputeCore(node, dirA, dirB, angleDeg, angleDeg, radiusMm, layingLengthMm, layingLengthMm, 0.001);

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
        => ComputeCore(node, dirA, dirB, angleDeg, angleDeg, radiusMm, layingLengthAMm, layingLengthBMm, mmToCoordinate);

    /// <summary>
    /// 실제 선형 편각과 선정된 표준 곡관각이 다른 경우의 5점 배치점 계산.
    /// P2/P4의 접선거리는 표준 곡관의 R·tan(θ/2)을 유지하고, P3는 실제 두 직선에 접하는 원호 위에 둔다.
    /// </summary>
    public static BendArcPoints ComputeForAlignment(
        Point3D node,
        Vector3D dirA,
        Vector3D dirB,
        double fittingAngleDeg,
        double alignmentDeflectionDeg,
        double radiusMm,
        double layingLengthAMm,
        double layingLengthBMm,
        double mmToCoordinate = 0.001)
        => ComputeCore(node, dirA, dirB, fittingAngleDeg, alignmentDeflectionDeg, radiusMm, layingLengthAMm, layingLengthBMm, mmToCoordinate);

    private static BendArcPoints ComputeCore(
        Point3D node,
        Vector3D dirA,
        Vector3D dirB,
        double fittingAngleDeg,
        double alignmentDeflectionDeg,
        double radiusMm,
        double layingLengthAMm,
        double layingLengthBMm,
        double mmToCoordinate)
    {
        // V에서 호 쪽을 향하는 내각 이등분선. 두 방향이 정반대(편각 0)면 정의되지 않는다.
        var sum = new Vector3D(dirA.X + dirB.X, dirA.Y + dirB.Y, dirA.Z + dirB.Z);
        if (sum.Length <= DegenerateEpsilon)
            throw new ArgumentException("편각이 0이라 곡관 이등분선을 정의할 수 없다.", nameof(alignmentDeflectionDeg));
        var bisector = sum.Normalize();

        var tangentMm = BendResolver.TangentLength(radiusMm, fittingAngleDeg);
        // P2/P4가 절점에서 T만큼 떨어진 상태에서 실제 두 직선에 접하는 원의 IP→호중점 거리.
        // 실제 편각과 표준각이 같으면 T·tan(θ/4) = R·(sec(θ/2)−1)로 기존 외거 E와 동일하다.
        var externalMm = tangentMm * Math.Tan(alignmentDeflectionDeg * Math.PI / 720d);
        var e = externalMm * mmToCoordinate;
        var t = tangentMm * mmToCoordinate;

        return new BendArcPoints(
            Offset(node, dirA, layingLengthAMm * mmToCoordinate),
            Offset(node, dirA, t),
            Offset(node, bisector, e),
            Offset(node, dirB, t),
            Offset(node, dirB, layingLengthBMm * mmToCoordinate),
            externalMm,
            tangentMm);
    }

    /// <summary>외거 E = R·(sec(θ/2) − 1). 절점에서 호 중점까지의 거리(mm).</summary>
    public static double ExternalDistance(double radiusMm, double angleDeg)
        => radiusMm * (1d / Math.Cos(angleDeg * Math.PI / 360d) - 1d);

    private static Point3D Offset(Point3D origin, Vector3D direction, double distance)
        => new(origin.X + direction.X * distance, origin.Y + direction.Y * distance, origin.Z + direction.Z * distance);
}
