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

    public void CreateNetwork(PipeNetworkDefinition network)
    {
        var document = _document() ?? throw new InvalidOperationException("활성 Revit 문서가 없습니다.");
        var systemType = new FilteredElementCollector(document).OfClass(typeof(PipingSystemType)).Cast<PipingSystemType>().FirstOrDefault()
            ?? throw new InvalidOperationException("프로젝트에 배관 시스템 타입이 없습니다.");
        var pipeType = new FilteredElementCollector(document).OfClass(typeof(PipeType)).Cast<PipeType>().FirstOrDefault()
            ?? throw new InvalidOperationException("프로젝트에 PipeType이 없습니다.");
        var level = new FilteredElementCollector(document).OfClass(typeof(Level)).Cast<Level>().OrderBy(x => x.Elevation).FirstOrDefault()
            ?? throw new InvalidOperationException("프로젝트에 Level이 없습니다.");
        var pipes = new Dictionary<Guid, Pipe>();
        foreach (var edge in network.Edges)
        {
            var pipe = Pipe.Create(document, systemType.Id, pipeType.Id, level.Id, ToXyz(edge.Start, edge.Elevation, network.ReferencePoint), ToXyz(edge.End, edge.Elevation, network.ReferencePoint));
            pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM)?.Set(UC.MmToFt(network.DiameterMm));
            pipes.Add(edge.Id, pipe);
        }
        foreach (var node in network.Nodes) ConnectNode(document, node, network, pipes);
        foreach (var edge in network.Edges) PlaceInlineFittings(document, edge, network, pipes[edge.Id]);
    }

    /// <summary>
    /// 엣지 위 배관부속(밸브 등)을 시작점에서 가까운 순서로 배치한다. 각 부속마다 파이프를 그 지점에서
    /// 분할(<see cref="PlumbingUtils.BreakCurve"/>)하고, 남는 두 커넥터에 부속 패밀리를 끼워 연결한다.
    /// docs/26 §5-2 미결정 1: BreakCurve가 원본 ElementId를 시작쪽(첫 조각)에 유지한다는 가정으로 작성했다
    /// — Revit 실행 검증에서 반대로 확인되면 currentPipe 갱신 순서를 뒤집어야 한다.
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
            var point = ToXyz(fitting.Position, fitting.Elevation, network.ReferencePoint);
            var symbol = FindSymbol(document, fitting.FamilyTypeName)
                ?? throw new InvalidOperationException($"배관부속 패밀리 '{fitting.FamilyTypeName}'를 찾을 수 없습니다. 프로젝트에 로드돼 있는지 확인하세요.");
            if (!symbol.IsActive) { symbol.Activate(); document.Regenerate(); }

            var newPipeId = PlumbingUtils.BreakCurve(document, currentPipe.Id, point);
            document.Regenerate();
            var beforeConnector = FindOpenConnectorNear(currentPipe, point);
            var afterPipe = (Pipe)document.GetElement(newPipeId);
            var afterConnector = FindOpenConnectorNear(afterPipe, point);
            if (beforeConnector is null || afterConnector is null)
                throw new InvalidOperationException("파이프 분할 지점에서 열린 커넥터를 찾지 못했습니다.");

            var instance = document.Create.NewFamilyInstance(point, symbol, StructuralType.NonStructural);
            document.Regenerate();
            AlignFittingToPipe(document, instance, point, direction);

            var instanceConnectors = GetMepConnectors(instance);
            if (instanceConnectors.Count != 2)
                throw new InvalidOperationException($"배관부속 '{fitting.FamilyTypeName}'의 커넥터 개수가 2개가 아닙니다. Pipe Accessory 패밀리인지 확인하세요.");
            var ordered = instanceConnectors.OrderBy(c => (c.Origin - point).DotProduct(direction)).ToList();
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
        var connected = network.Edges.Where(x => IsAt(x.Start, node.Position) || IsAt(x.End, node.Position)).Select(x => FindConnector(pipes[x.Id], node.Position, x.Elevation, network.ReferencePoint)).Where(x => x is not null).Cast<Connector>().ToList();
        try
        {
            switch (node.NodeKind)
            {
                case NodeKind.Elbow when connected.Count == 2: document.Create.NewElbowFitting(connected[0], connected[1]); break;
                case NodeKind.Tee when connected.Count == 3: document.Create.NewTeeFitting(connected[0], connected[1], connected[2]); break;
                case NodeKind.Cross when connected.Count == 4: document.Create.NewCrossFitting(connected[0], connected[1], connected[2], connected[3]); break;
                case NodeKind.Inline when connected.Count == 2: document.Create.NewUnionFitting(connected[0], connected[1]); break;
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
