using DHBIMWATER.Core.Geometry;

namespace DHBIMWATER.Core.Gis;

/// <summary>절점에 들어갈 부재 종류. 곡관 각도 선정은 허용굴곡 설정이 필요하므로 별도(BendResolver) 단계에서 결정한다.</summary>
public enum NodeKind
{
    /// <summary>접속 간선이 없는 고립 절점(정상 데이터에서는 나오지 않는다).</summary>
    Isolated,
    /// <summary>차수 1 — 관로 단부(맹판/캡).</summary>
    EndPoint,
    /// <summary>차수 2 — 편각·직경이 모두 그대로인 통과점(부재 없음).</summary>
    Straight,
    /// <summary>차수 2 — 편각은 없고 직경만 바뀌는 점(레듀서).</summary>
    Reducer,
    /// <summary>차수 2 — 편각이 있는 점(곡관 후보).</summary>
    Bend,
    /// <summary>차수 3 — 세 방향 직경이 같은 분기.</summary>
    Tee,
    /// <summary>차수 3 — 직경이 다른 분기(이경티).</summary>
    ReducingTee,
    /// <summary>차수 4 — 십자.</summary>
    Cross,
    /// <summary>차수 5 이상 — 단일 부재로 표현할 수 없어 사용자 확인이 필요하다.</summary>
    TooManyBranches
}

/// <param name="DeflectionDeg">차수 2에서 이전·다음 선분 사이의 편각(0=직선). 그 외 차수에서는 0.</param>
/// <param name="MaxDiameterMm">접속 간선 중 최대 직경(주관 판단용).</param>
/// <param name="MinDiameterMm">접속 간선 중 최소 직경.</param>
public sealed record NodeClassification(
    int NodeId,
    Point3D Position,
    NodeKind Kind,
    int Degree,
    double DeflectionDeg,
    double MaxDiameterMm,
    double MinDiameterMm)
{
    public bool DiameterChanged => MaxDiameterMm - MinDiameterMm > 1e-9;
}

/// <summary>그래프의 절점을 차수와 직경 조합으로 분류한다. Revit 의존이 없는 순수 계산이다.</summary>
public static class PipeNetworkClassifier
{
    /// <summary>이 값 이하의 편각은 직선으로 본다(부동소수 오차 흡수용이며, 곡관 생략 판단은 허용굴곡 설정이 담당한다).</summary>
    private const double StraightEpsilonDeg = 1e-6;

    public static IReadOnlyList<NodeClassification> Classify(PipeNetworkGraph graph)
    {
        var result = new List<NodeClassification>(graph.Nodes.Count);
        foreach (var node in graph.Nodes)
        {
            var diameters = node.Incidences.Select(x => graph.Edges[x.EdgeId].DiameterMm).ToList();
            var max = diameters.Count == 0 ? 0 : diameters.Max();
            var min = diameters.Count == 0 ? 0 : diameters.Min();
            var deflection = node.Degree == 2 ? DeflectionDegrees(graph, node) : 0d;
            var kind = Resolve(node.Degree, deflection, max - min > 1e-9);
            result.Add(new NodeClassification(node.Id, node.Position, kind, node.Degree, deflection, max, min));
        }
        return result;
    }

    private static NodeKind Resolve(int degree, double deflectionDeg, bool diameterChanged) => degree switch
    {
        0 => NodeKind.Isolated,
        1 => NodeKind.EndPoint,
        2 when deflectionDeg > StraightEpsilonDeg => NodeKind.Bend,
        2 when diameterChanged => NodeKind.Reducer,
        2 => NodeKind.Straight,
        3 => diameterChanged ? NodeKind.ReducingTee : NodeKind.Tee,
        4 => NodeKind.Cross,
        _ => NodeKind.TooManyBranches
    };

    /// <summary>
    /// 차수 2 절점의 편각(도). 절점에서 양쪽으로 나가는 두 단위벡터의 사잇각이 180°면 편각 0이다.
    /// 사용자 결정에 따라 수평/수직을 분리하지 않고 3D 입체각 하나로 계산한다.
    /// </summary>
    public static double DeflectionDegrees(PipeNetworkGraph graph, NetworkNode node)
    {
        if (node.Degree != 2) throw new ArgumentException("편각은 차수 2 절점에서만 정의된다.", nameof(node));
        var a = graph.DirectionFrom(node.Incidences[0]);
        var b = graph.DirectionFrom(node.Incidences[1]);
        var dot = Math.Clamp(a.X * b.X + a.Y * b.Y + a.Z * b.Z, -1d, 1d);
        return 180d - Math.Acos(dot) * 180d / Math.PI;
    }
}
