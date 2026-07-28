namespace DHBIMWATER.Core.Gis;

public static class AlignmentReferencePoint
{
    /// <summary>첫 유효 정점을 정수 미터로 반올림한 값을 내부원점 기준점으로 사용한다.</summary>
    public static (double X, double Y, double Z)? FromFirstVertex(IEnumerable<PipeAlignment> alignments)
    {
        var vertex = alignments.SelectMany(x => x.Vertices).FirstOrDefault();
        if (vertex is null) return null;
        return (
            Math.Round(vertex.X, MidpointRounding.AwayFromZero),
            Math.Round(vertex.Y, MidpointRounding.AwayFromZero),
            Math.Round(vertex.Z, MidpointRounding.AwayFromZero));
    }
}
