namespace DHBIMWATER.Core.Gis;

public static class AlignmentReferencePoint
{
    /// <summary>
    /// 첫 유효 정점의 평면좌표를 기준점으로 사용한다.
    /// 이 기준점은 프로젝트 기준점(PBP) 위치에 그대로 대응하므로 반올림은 부동소수 오차 제거 수준(소수 5자리)으로만 한다.
    /// 표고는 기준점에 포함하지 않는다 — 모델 높이는 정점 Z값으로만 결정한다.
    /// </summary>
    public static (double X, double Y)? FromFirstVertex(IEnumerable<PipeAlignment> alignments)
    {
        var vertex = alignments.SelectMany(x => x.Vertices).FirstOrDefault();
        if (vertex is null) return null;
        return (
            Math.Round(vertex.X, 5, MidpointRounding.AwayFromZero),
            Math.Round(vertex.Y, 5, MidpointRounding.AwayFromZero));
    }
}
