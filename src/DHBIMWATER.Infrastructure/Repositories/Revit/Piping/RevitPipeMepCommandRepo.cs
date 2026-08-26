using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Piping;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Piping;

/// <summary>MEP Pipe API로 정규화된 그래프를 생성하고 노드 차수에 맞는 표준 부속을 연결한다.</summary>
internal sealed class RevitPipeMepCommandRepo : IPipeCommandRepo
{
    private readonly Func<Document?> _document;
    public RevitPipeMepCommandRepo(Func<Document?> document) => _document = document;
    public PipeOutputMode OutputMode => PipeOutputMode.MepPipe;

    public PipeCreationResult CreateNetwork(PipeNetworkDefinition network)
    {
        var document = _document() ?? throw new InvalidOperationException("활성 Revit 문서가 없습니다.");
        var systemType = new FilteredElementCollector(document).OfClass(typeof(PipingSystemType)).Cast<PipingSystemType>()
            .FirstOrDefault(x => x.Name == network.PipingSystemTypeName)
            ?? throw new InvalidOperationException($"배관 시스템 타입 '{network.PipingSystemTypeName}'을 찾을 수 없습니다.");
        var pipeType = new FilteredElementCollector(document).OfClass(typeof(PipeType)).Cast<PipeType>()
            .FirstOrDefault(x => x.Name == network.PipeTypeName)
            ?? throw new InvalidOperationException($"PipeType '{network.PipeTypeName}'을 찾을 수 없습니다.");
        var level = new FilteredElementCollector(document).OfClass(typeof(Level)).Cast<Level>()
            .FirstOrDefault(x => x.Name == network.LevelName)
            ?? throw new InvalidOperationException($"레벨 '{network.LevelName}'을 찾을 수 없습니다.");
        var pipes = new Dictionary<Guid, Pipe>();
        foreach (var edge in network.Edges)
        {
            var pipe = Pipe.Create(document, systemType.Id, pipeType.Id, level.Id, ToXyz(edge.Start, edge.Elevation, network.ReferencePoint), ToXyz(edge.End, edge.Elevation, network.ReferencePoint));
            pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM)?.Set(UC.MmToFt(network.DiameterMm));
            pipes.Add(edge.Id, pipe);
        }
        foreach (var node in network.Nodes) ConnectNode(document, node, network, pipes);
        foreach (var edge in network.Edges) PlaceInlineFittings(document, edge, network, pipes[edge.Id]);
        return new PipeCreationResult($"배관 {network.Edges.Count}개를 생성했습니다.");
    }

    /// <summary>
    /// 엣지 위 배관부속(밸브 등)을 시작점에서 가까운 순서로 배치한다. 부속 인스턴스를 먼저 만들어 방향을 맞춘 뒤
    /// 파이프를 그 지점에서 분할(<see cref="PlumbingUtils.BreakCurve"/>)하고, 남는 두 커넥터에 부속을 끼워 연결한다.
    /// <para>인스턴스 생성·정렬을 분할보다 <b>먼저</b> 하는 이유 — 분할 직후에는 분할 지점에 열린 커넥터 2개가 생기는데
    /// 그 자리에 부속을 만들면 Revit이 자동 연결해 버리고, 연결된 상태에서 회전·이동하면 파이프가 뒤집히면서
    /// "덕트/배관이 반대 방향으로 수정되어 연결이 무효화됩니다" 예외로 트랜잭션 전체가 실패한다.</para>
    /// <para>docs/26 §5-2 미결정 1(BreakCurve가 원본 ElementId를 어느 조각에 남기는지)은 가정하지 않고
    /// 두 조각의 실제 좌표로 시작쪽·끝쪽을 판별한다.</para>
    /// </summary>
    private static void PlaceInlineFittings(Document document, PipeEdgeDefinition edge, PipeNetworkDefinition network, Pipe initialPipe)
    {
        var fittings = network.InlineFittings
            .Where(x => x.EdgeId == edge.Id && !string.IsNullOrWhiteSpace(x.FamilyTypeName))
            .OrderBy(x => x.Position.DistanceTo(edge.Start))
            .ToList();
        if (fittings.Count == 0) return;

        var startXyz = ToXyz(edge.Start, edge.Elevation, network.ReferencePoint);
        var endXyz = ToXyz(edge.End, edge.Elevation, network.ReferencePoint);
        var direction = (endXyz - startXyz).Normalize();
        var currentPipe = initialPipe;

        foreach (var fitting in fittings)
        {
            var rawPoint = ToXyz(fitting.Position, fitting.Elevation, network.ReferencePoint);
            // ConnectNode가 앞서 엘보/티 부속을 연결하며 파이프 양 끝을 트리밍했을 수 있어, 원래 계산된 좌표가
            // 더 이상 실제 파이프 커브 위에 있지 않을 수 있다(BreakCurve는 "point not on curve"로 실패한다).
            // 현재 파이프의 실제 커브에 투영한 점을 사용해 항상 유효한 좌표를 넘긴다.
            var pipeCurve = ((LocationCurve)currentPipe.Location).Curve;
            var point = pipeCurve.Project(rawPoint).XYZPoint;
            var symbol = FindSymbol(document, fitting.FamilyTypeName)
                ?? throw new InvalidOperationException($"배관부속 패밀리 '{fitting.FamilyTypeName}'를 찾을 수 없습니다. 프로젝트에 로드돼 있는지 확인하세요.");
            if (!symbol.IsActive) { symbol.Activate(); document.Regenerate(); }

            // 1) 분할 전에 인스턴스를 만들고 방향을 맞춘다(자동 연결 방지 — 위 요약 참조).
            var instance = document.Create.NewFamilyInstance(point, symbol, StructuralType.NonStructural);
            document.Regenerate();
            DisconnectAll(instance);
            AlignFittingToPipe(document, instance, point, direction);

            var instanceConnectors = GetMepConnectors(instance);
            if (instanceConnectors.Count != 2)
                throw new InvalidOperationException($"배관부속 '{fitting.FamilyTypeName}'의 커넥터 개수가 2개가 아닙니다. Pipe Accessory 패밀리인지 확인하세요.");
            var ordered = instanceConnectors.OrderBy(c => (c.Origin - point).DotProduct(direction)).ToList();

            // 2) 연결하면 부속 반길이만큼 양쪽 파이프가 줄어든다. 남는 길이가 모자라면 Revit이 파이프를 뒤집으면서
            //    "반대 방향으로 수정되어 연결이 무효화됩니다"로 실패하므로, 그 전에 원인을 알 수 있는 예외를 낸다.
            EnsureStubLength(pipeCurve, point, direction, ordered[0].Origin.DistanceTo(ordered[1].Origin) / 2, fitting.FamilyTypeName);

            // 3) 분할한 뒤 두 조각의 실제 좌표로 시작쪽·끝쪽을 판별한다(BreakCurve의 Id 유지 규칙을 가정하지 않는다).
            var newPipeId = PlumbingUtils.BreakCurve(document, currentPipe.Id, point);
            document.Regenerate();
            var splitPipe = document.GetElement(newPipeId) as Pipe
                ?? throw new InvalidOperationException("파이프 분할 결과를 찾지 못했습니다.");
            var (beforePipe, afterPipe) = OffsetAlong(currentPipe, startXyz, direction) <= OffsetAlong(splitPipe, startXyz, direction)
                ? (currentPipe, splitPipe)
                : (splitPipe, currentPipe);

            var beforeConnector = FindOpenConnectorNear(beforePipe, point);
            var afterConnector = FindOpenConnectorNear(afterPipe, point);
            if (beforeConnector is null || afterConnector is null)
                throw new InvalidOperationException("파이프 분할 지점에서 열린 커넥터를 찾지 못했습니다.");

            ordered[0].ConnectTo(beforeConnector);
            ordered[1].ConnectTo(afterConnector);

            currentPipe = afterPipe;
        }
    }

    /// <summary>부속 패밀리를 파이프 진행 방향에 맞춰 회전시키고, 커넥터 중점이 분할 지점에 오도록 이동한다.
    /// docs/26 §5-2 미결정 2 — Z축 기준 회전만 다루므로 배관이 수평(평면 스케치)일 때만 유효하다.</summary>
    private static void AlignFittingToPipe(Document document, FamilyInstance instance, XYZ point, XYZ direction)
    {
        var connectors = GetMepConnectors(instance);
        if (connectors.Count != 2) return;
        var currentDirection = connectors[1].Origin - connectors[0].Origin;
        if (currentDirection.GetLength() > 1e-6)
        {
            currentDirection = currentDirection.Normalize();
            var angle = currentDirection.AngleTo(direction);
            if (angle > 1e-6)
            {
                var cross = currentDirection.CrossProduct(direction);
                var signedAngle = cross.DotProduct(XYZ.BasisZ) < 0 ? -angle : angle;
                ElementTransformUtils.RotateElement(document, instance.Id, Line.CreateBound(point, point + XYZ.BasisZ), signedAngle);
                document.Regenerate();
            }
        }
        connectors = GetMepConnectors(instance);
        var midpoint = (connectors[0].Origin + connectors[1].Origin) / 2;
        var offset = point - midpoint;
        if (offset.GetLength() > 1e-6)
        {
            ElementTransformUtils.MoveElement(document, instance.Id, offset);
            document.Regenerate();
        }
    }

    /// <summary>배치 직후 Revit이 근처 열린 커넥터에 자동 연결했을 수 있다. 연결된 채로 회전·이동하면 파이프가
    /// 뒤집히며 트랜잭션이 실패하므로, 정렬 전에 물리 연결을 모두 끊는다(연결이 없으면 아무 일도 하지 않는다).</summary>
    private static void DisconnectAll(FamilyInstance instance)
    {
        foreach (var connector in GetMepConnectors(instance))
            foreach (var reference in connector.AllRefs.Cast<Connector>()
                         .Where(x => x.ConnectorType is ConnectorType.End or ConnectorType.Curve).ToList())
                if (connector.IsConnectedTo(reference)) connector.DisconnectFrom(reference);
    }

    /// <summary>부속 양쪽에 남는 파이프 길이가 부속 반길이보다 짧으면, Revit의 모호한 "반대 방향" 오류 대신
    /// 어느 부속을 어디로 옮겨야 하는지 알 수 있는 예외를 낸다.</summary>
    private static void EnsureStubLength(Curve pipeCurve, XYZ point, XYZ direction, double halfLength, string familyTypeName)
    {
        var offsets = new[] { (pipeCurve.GetEndPoint(0) - point).DotProduct(direction), (pipeCurve.GetEndPoint(1) - point).DotProduct(direction) };
        var available = Math.Min(Math.Abs(offsets.Min()), Math.Abs(offsets.Max()));
        if (available >= halfLength) return;
        throw new InvalidOperationException(
            $"부속 '{familyTypeName}'을(를) 놓을 자리가 부족합니다. 배치 지점 양쪽에 부속 길이의 절반" +
            $"({UC.FtToMm(halfLength):N0}mm) 이상이 남아야 하는데 현재 여유는 {UC.FtToMm(available):N0}mm입니다. " +
            "부속을 선 가운데 쪽으로 옮기거나 배관을 더 길게 그리세요.");
    }

    /// <summary>파이프 중점이 <paramref name="origin"/>에서 진행 방향으로 얼마나 떨어져 있는지. 분할된 두 조각의
    /// 시작쪽·끝쪽 판별에 쓴다.</summary>
    private static double OffsetAlong(Pipe pipe, XYZ origin, XYZ direction)
    {
        var curve = ((LocationCurve)pipe.Location).Curve;
        return (((curve.GetEndPoint(0) + curve.GetEndPoint(1)) / 2) - origin).DotProduct(direction);
    }

    private static List<Connector> GetMepConnectors(FamilyInstance instance)
        => instance.MEPModel?.ConnectorManager?.Connectors.Cast<Connector>().ToList() ?? [];

    private static Connector? FindOpenConnectorNear(Pipe pipe, XYZ point)
        => pipe.ConnectorManager.Connectors.Cast<Connector>().Where(x => !x.IsConnected).OrderBy(x => x.Origin.DistanceTo(point)).FirstOrDefault();

    private static FamilySymbol? FindSymbol(Document document, string familyTypeName)
    {
        var separator = familyTypeName.LastIndexOf(" : ", StringComparison.Ordinal);
        if (separator <= 0 || separator >= familyTypeName.Length - 3) return null;
        var familyName = familyTypeName[..separator];
        var typeName = familyTypeName[(separator + 3)..];
        return new FilteredElementCollector(document).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>()
            .FirstOrDefault(x => x.Family.Name == familyName && x.Name == typeName);
    }

    private static void ConnectNode(Document document, PipeNodeDefinition node, PipeNetworkDefinition network, IReadOnlyDictionary<Guid, Pipe> pipes)
    {
        var connected = network.Edges.Where(x => IsAt(x.Start, node.Position) || IsAt(x.End, node.Position))
            .Select(x => (Edge: x, Connector: FindConnector(pipes[x.Id], node.Position, x.Elevation, network.ReferencePoint)))
            .Where(x => x.Connector is not null)
            .ToDictionary(x => x.Edge.Id, x => x.Connector!);
        try
        {
            switch (node.NodeKind)
            {
                case NodeKind.Elbow when connected.Count == 2:
                    document.Create.NewElbowFitting(connected.Values.ElementAt(0), connected.Values.ElementAt(1));
                    break;
                case NodeKind.Tee when connected.Count == 3:
                    var order = PipeTeeResolver.Resolve(node, network.Edges)
                        ?? throw new InvalidOperationException("직교 T로 판별할 수 없습니다. 주관은 일직선, 분기관은 90°로 그려야 합니다.");
                    document.Create.NewTeeFitting(connected[order.MainEdge1Id], connected[order.MainEdge2Id], connected[order.BranchEdgeId]);
                    break;
                case NodeKind.Cross when connected.Count == 4:
                    document.Create.NewCrossFitting(connected.Values.ElementAt(0), connected.Values.ElementAt(1), connected.Values.ElementAt(2), connected.Values.ElementAt(3));
                    break;
                case NodeKind.Inline when connected.Count == 2:
                    document.Create.NewUnionFitting(connected.Values.ElementAt(0), connected.Values.ElementAt(1));
                    break;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"노드 {node.NodeKind} 부속 연결에 실패했습니다. 선택한 PipeType에 해당 부속이 있는지 확인하세요.", ex);
        }
    }

    private static Connector? FindConnector(Pipe pipe, DHBIMWATER.Core.Geometry.Point2D point, double elevation, DHBIMWATER.Core.Geometry.Point2D referencePoint)
    {
        var target = ToXyz(point, elevation, referencePoint);
        return pipe.ConnectorManager.Connectors.Cast<Connector>().OrderBy(x => x.Origin.DistanceTo(target)).FirstOrDefault();
    }
    private static bool IsAt(DHBIMWATER.Core.Geometry.Point2D point, DHBIMWATER.Core.Geometry.Point2D node) => point.DistanceTo(node) <= 0.01;
    private static XYZ ToXyz(DHBIMWATER.Core.Geometry.Point2D point, double elevation, DHBIMWATER.Core.Geometry.Point2D referencePoint) => new(UC.MmToFt(point.X + referencePoint.X), UC.MmToFt(point.Y + referencePoint.Y), UC.MmToFt(elevation));
}
