using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Application.Gis;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Core.Gis;

namespace DHBIMWATER.Application.UseCases.Gis;

/// <summary>
/// 관로 폴리선을 위상 그래프로 만들어 절점을 분류하고 곡관을 판정한다.
/// 읽기 전용이므로 ITransactionContext를 주입받지 않는다.
/// </summary>
public sealed class AnalyzePipeNetworkUseCase
{
    private readonly AlignmentSourceLoader _loader;
    private readonly BendSettingsProvider _settings;

    public AnalyzePipeNetworkUseCase(AlignmentSourceLoader loader, BendSettingsProvider settings)
    { _loader = loader; _settings = settings; }

    public PipeNetworkDiagnosisResult Execute(PipeNetworkDiagnosisRequest request)
    {
        if (request.SnapToleranceMm <= 0) throw new ArgumentOutOfRangeException(nameof(request.SnapToleranceMm));

        var loaded = _loader.Load(request.Files);
        var warnings = loaded.Warnings.ToList();

        var settingsResolution = request.CurrentBendSettings is null
            ? _settings.Load()
            : new BendSettingsResolution(request.CurrentBendSettings, BendSettingsSource.Project);
        var (settings, source) = settingsResolution;
        if (source == BendSettingsSource.BuiltInDefault)
            warnings.Add("관로 규격 설정이 저장되지 않아 내장 기본값으로 판정했습니다.");
        if (settings.Fittings.NeedsRestore)
            warnings.Add("저장된 곡관 치수가 현행 규격표와 구조가 다릅니다(중복 행 또는 플랜지곡관 누락). [관·곡관 규격표] 창에서 [기본값 복원] 후 저장하세요.");

        var graph = PipeNetworkBuilder.Build(loaded.Alignments, request.SnapToleranceMm / 1000d);
        var nodes = PipeNetworkClassifier.Classify(graph);
        var resolutions = BendResolver.ResolveAll(nodes, settings).ToDictionary(x => x.NodeId);

        var standard = resolutions.Values.Count(x => x.Kind == BendResolutionKind.Standard);
        var none = resolutions.Values.Count(x => x.Kind == BendResolutionKind.None);
        var unresolved = resolutions.Values.Count(x => x.Kind == BendResolutionKind.Unresolved);
        var conflicts = resolutions.Values.Where(x => !x.IsSizeConsistent).ToList();

        if (resolutions.Values.Any(x => x.Kind != BendResolutionKind.None) && resolutions.Values.All(x => !x.HasFittingSize))
            warnings.Add("곡관 치수가 입력되지 않아 직관 구간 차감이 적용되지 않습니다.");

        foreach (var conflict in conflicts)
            warnings.Add($"절점 {conflict.NodeId}: 곡관 치수가 성립하지 않습니다(t={conflict.LayingLengthMm:0.#}mm < 접선길이 T={conflict.TangentLengthMm:0.#}mm). 호가 곡관 몸통 밖으로 나갑니다.");

        warnings.AddRange(FindAdjacentInterference(graph, resolutions));

        return new PipeNetworkDiagnosisResult(
            graph.Nodes.Count,
            graph.Edges.Count,
            nodes.GroupBy(x => x.Kind.ToString()).ToDictionary(g => g.Key, g => g.Count()),
            standard, none, unresolved,
            conflicts.Count,
            BuildAttention(nodes, resolutions),
            warnings);
    }

    /// <summary>
    /// 인접한 두 절점의 곡관이 겹치는지 본다. 간선 길이보다 양끝 t의 합이 크면 그 사이 직관 구간이 음수가 된다.
    /// </summary>
    private static IEnumerable<string> FindAdjacentInterference(PipeNetworkGraph graph, IReadOnlyDictionary<int, BendResolution> resolutions)
    {
        foreach (var edge in graph.Edges)
        {
            var head = LayingLength(resolutions, edge.StartNodeId);
            var tail = LayingLength(resolutions, edge.EndNodeId);
            if (head + tail <= 0) continue;

            var lengthMm = edge.Length * 1000d;
            if (head + tail > lengthMm)
                yield return $"절점 {edge.StartNodeId}–{edge.EndNodeId}: 구간 길이 {lengthMm:0.#}mm 가 양끝 곡관 연장 합계 {head + tail:0.#}mm 보다 짧아 직관이 들어갈 자리가 없습니다.";
        }
    }

    private static double LayingLength(IReadOnlyDictionary<int, BendResolution> resolutions, int nodeId)
        => resolutions.TryGetValue(nodeId, out var r) && r.Kind != BendResolutionKind.None ? r.LayingLengthMm : 0d;

    /// <summary>사용자가 눈으로 확인해야 하는 절점만 담는다. 정상 Tee/Cross/Straight는 집계에만 반영한다.</summary>
    private static IReadOnlyList<PipeNetworkNodeReport> BuildAttention(IReadOnlyList<NodeClassification> nodes, IReadOnlyDictionary<int, BendResolution> resolutions)
    {
        var reports = new List<(int Order, PipeNetworkNodeReport Report)>();
        foreach (var node in nodes)
        {
            resolutions.TryGetValue(node.NodeId, out var bend);
            var order = node.Kind switch
            {
                _ when bend is { Kind: BendResolutionKind.Unresolved } => 0,
                _ when bend is { IsSizeConsistent: false } => 1,
                NodeKind.TooManyBranches => 2,
                NodeKind.EndPoint => 3,
                _ => -1
            };
            if (order < 0) continue;
            reports.Add((order, ToReport(node, bend)));
        }
        return reports.OrderBy(x => x.Order).ThenBy(x => x.Report.NodeId).Select(x => x.Report).ToList();
    }

    private static PipeNetworkNodeReport ToReport(NodeClassification node, BendResolution? bend) => new(
        node.NodeId, node.Kind.ToString(), node.Degree,
        node.Position.X, node.Position.Y, node.Position.Z,
        node.DeflectionDeg, node.MaxDiameterMm, node.MinDiameterMm, node.PipeKind,
        bend?.Kind.ToString() ?? string.Empty,
        bend?.StandardAngleDeg ?? 0d, bend?.ResidualDeg ?? 0d,
        bend?.LayingLengthMm ?? 0d, bend?.CenterlineRadiusMm ?? 0d, bend?.TangentLengthMm ?? 0d,
        bend?.IsSizeConsistent ?? true);

}
