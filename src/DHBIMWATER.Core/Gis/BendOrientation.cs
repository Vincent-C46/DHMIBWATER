using DHBIMWATER.Core.Geometry;

namespace DHBIMWATER.Core.Gis;

/// <summary>
/// 5점 가변 곡관 패밀리의 P1~P5 각 점에서 진행방향 접선과 rot_XY/rot_XZ 인스턴스 매개변수(점별 5쌍)를 계산한다.
/// Revit 의존 없는 순수 계산이다.
/// </summary>
/// <remarks>
/// 가정(2026-08-11, 검증 필요): rot_XY는 접선을 XY평면에 투영한 방향각(+X축 기준, Z축 회전),
/// rot_XZ는 그 투영 대비 수직 기울기각이다. 패밀리 로컬축 기준·부호는 RFA 완성 후 Revit에서 직접
/// 대조해야 하며, 맞지 않으면 <see cref="Compute"/>만 고치면 된다.
/// </remarks>
public static class BendOrientation
{
    private const double DegenerateEpsilon = 1e-9;

    /// <summary>
    /// P1~P5 각 점의 진행방향 접선 벡터. P1·P2는 진입 직선(dirA 반대) 방향, P4·P5는 진출 직선(dirB) 방향으로
    /// 같은 값을 쓰고(직선 구간은 뒤틀리지 않는다는 전제), P3(호 중간점)만 두 방향 사이를 로드리게스 회전공식으로
    /// θ/2만큼 정확히 회전시켜 구한다.
    /// </summary>
    /// <param name="dirA">절점에서 상류(P1 쪽) 방향 단위벡터.</param>
    /// <param name="dirB">절점에서 하류(P5 쪽) 방향 단위벡터.</param>
    /// <param name="angleDeg">곡관 각도 θ(편각).</param>
    public static IReadOnlyList<Vector3D> Tangents(Vector3D dirA, Vector3D dirB, double angleDeg)
    {
        var forwardA = new Vector3D(-dirA.X, -dirA.Y, -dirA.Z); // 진입 방향(=흐름이 절점으로 들어오는 방향)
        var forwardB = dirB;                                    // 진출 방향(=흐름이 절점을 빠져나가는 방향)

        var axis = Cross(forwardA, forwardB);
        var axisLength = Length(axis);
        // 편각이 0/180에 가까워 축을 정할 수 없으면(직선에 가까움) 회전 없이 진입 방향을 그대로 쓴다.
        var mid = axisLength <= DegenerateEpsilon
            ? forwardA
            : Rotate(forwardA, Scale(axis, 1d / axisLength), angleDeg * Math.PI / 360d);

        return new[] { forwardA, forwardA, mid, forwardB, forwardB };
    }

    /// <summary>접선 벡터 하나를 rot_XY/rot_XZ(도)로 환산한다.</summary>
    public static (double RotXYDeg, double RotXZDeg) Compute(Vector3D forward)
    {
        var horizontal = Math.Sqrt(forward.X * forward.X + forward.Y * forward.Y);
        var rotXy = Math.Atan2(forward.Y, forward.X) * 180d / Math.PI;
        var rotXz = Math.Atan2(forward.Z, horizontal) * 180d / Math.PI;
        return (rotXy, rotXz);
    }

    private static Vector3D Cross(Vector3D a, Vector3D b)
        => new(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);

    private static double Dot(Vector3D a, Vector3D b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    private static double Length(Vector3D v) => Math.Sqrt(Dot(v, v));

    private static Vector3D Scale(Vector3D v, double s) => new(v.X * s, v.Y * s, v.Z * s);

    /// <summary>로드리게스 회전공식. v를 단위축 axis 기준으로 angleRad만큼 회전한다.</summary>
    private static Vector3D Rotate(Vector3D v, Vector3D axis, double angleRad)
    {
        var cos = Math.Cos(angleRad);
        var sin = Math.Sin(angleRad);
        var cross = Cross(axis, v);
        var dot = Dot(axis, v);
        return new Vector3D(
            v.X * cos + cross.X * sin + axis.X * dot * (1 - cos),
            v.Y * cos + cross.Y * sin + axis.Y * dot * (1 - cos),
            v.Z * cos + cross.Z * sin + axis.Z * dot * (1 - cos));
    }
}
