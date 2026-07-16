using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
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
