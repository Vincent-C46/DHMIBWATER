using DHBIMWATER.Core.Geometry;

namespace DHBIMWATER.Core.Gis;

/// <summary>
/// 2점 가변 직관 인스턴스 하나의 시작/끝 회전 기준 방향을 정한다. 직전 인스턴스와 같은 점에서 맞닿아 있으면
/// (같은 PipeAlignment, 좌표가 사실상 일치) 시작쪽 방향은 직전 인스턴스의 "고유 방향"을 그대로 물려받아
/// 두 인스턴스가 절점에서 같은 rot_XY/rot_XZ를 기록하게 한다. 끝쪽 방향은 항상 이 세그먼트 자신의 실제
/// 방향이다(다음 경계에서 이 값을 물려줄 "고유 방향"이 된다).
/// </summary>
public static class StraightSegmentOrientation
{
    private const double JoinToleranceM = 1e-6;

    public static (Vector3D Start, Vector3D Own) Resolve(
        Point3D segmentStart, Point3D segmentEnd,
        Point3D? previousEnd, Vector3D? previousOwnDirection)
    {
        var own = new Vector3D(
            segmentEnd.X - segmentStart.X,
            segmentEnd.Y - segmentStart.Y,
            segmentEnd.Z - segmentStart.Z);
        var joinsPrevious = previousEnd is { } end && previousOwnDirection is not null
            && end.DistanceTo(segmentStart) <= JoinToleranceM;
        var start = joinsPrevious ? previousOwnDirection! : own;
        return (start, own);
    }
}
