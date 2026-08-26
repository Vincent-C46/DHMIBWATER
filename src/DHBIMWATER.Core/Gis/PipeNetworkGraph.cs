using DHBIMWATER.Core.Geometry;

namespace DHBIMWATER.Core.Gis;

/// <summary>노드에 접속한 간선의 한쪽 끝. <see cref="AtStart"/>가 true면 간선의 시작점이 이 노드에 붙어 있다.</summary>
public sealed record NodeIncidence(int EdgeId, bool AtStart);

/// <summary>여러 폴리선의 정점/교차점을 스냅 허용오차로 병합한 관로 네트워크의 절점.</summary>
public sealed record NetworkNode(int Id, Point3D Position, IReadOnlyList<NodeIncidence> Incidences)
{
    /// <summary>접속 간선 수. 1=단부, 2=직선/곡관 후보, 3=T분기, 4=십자.</summary>
    public int Degree => Incidences.Count;
}

/// <summary>절점 사이를 잇는 직선 구간. 원본 폴리선(<see cref="AlignmentIndex"/>)의 한 선분이거나 그것을 분할한 조각이다.</summary>
public sealed record NetworkEdge(
    int Id,
    int AlignmentIndex,
    int StartNodeId,
    int EndNodeId,
    Point3D Start,
    Point3D End,
    double DiameterMm,
    string PipeKind)
{
    public double Length => Start.DistanceTo(End);
}

/// <summary>
/// 관로 폴리선 집합의 위상 그래프. 좌표 단위는 입력 <see cref="PipeAlignment"/>와 동일하다(원본 GIS 좌표 = m).
/// </summary>
public sealed class PipeNetworkGraph
{
    public PipeNetworkGraph(IReadOnlyList<NetworkNode> nodes, IReadOnlyList<NetworkEdge> edges)
    {
        Nodes = nodes;
        Edges = edges;
    }

    /// <summary>인덱스 = <see cref="NetworkNode.Id"/>.</summary>
    public IReadOnlyList<NetworkNode> Nodes { get; }

    /// <summary>인덱스 = <see cref="NetworkEdge.Id"/>.</summary>
    public IReadOnlyList<NetworkEdge> Edges { get; }

    /// <summary>노드에서 해당 간선을 따라 바깥으로 향하는 단위벡터. 편각 계산의 기준이다.</summary>
    public Vector3D DirectionFrom(NodeIncidence incidence)
    {
        var edge = Edges[incidence.EdgeId];
        var from = incidence.AtStart ? edge.Start : edge.End;
        var to = incidence.AtStart ? edge.End : edge.Start;
        return new Vector3D(to.X - from.X, to.Y - from.Y, to.Z - from.Z).Normalize();
    }
}
