using DHBIMWATER.Core.Piping;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;
using DHBIMWATER.UI.Utilities;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace DHBIMWATER.UI.ViewModels.Modeling;

public sealed class PipeLayoutViewModel : ViewModelBase
{
    private readonly PipeNetwork _network = new();
    private Point? _segmentStart;
    private PipeEdgeItem? _selectedEdge;
    private string _selectedFittingType = "밸브";
    private string _status = "캔버스를 클릭하여 배관 시작점을 지정하세요.";
    private double _elevation;
    private double _diameterMm = 100;
    private PipeOutputMode _outputMode = PipeOutputMode.MepPipe;
    private bool _useAngleSnap = true, _isPreviewVisible;
    private double _previewX1, _previewY1, _previewX2, _previewY2;

    public PipeLayoutViewModel()
    {
        FittingTypes = new ObservableCollection<string>(["밸브", "플랜지", "유량계", "감압밸브"]);
        Edges = [];
        Nodes = [];
        InlineFittings = [];
        AddFittingCommand = new RelayCommand(_ => AddFitting(), _ => SelectedEdge is not null);
        RemoveFittingCommand = new RelayCommand(x => RemoveFitting(x as InlineFittingItem));
        ClearCommand = new RelayCommand(_ => Clear());
        CancelDrawingCommand = new RelayCommand(_ => CancelDrawing());
        CloseCommand = new RelayCommand(_ => CloseAction?.Invoke());
        CreateModelCommand = new RelayCommand(_ => CreateModel(), _ => _network.Edges.Count > 0 && CreateModelAction is not null);
    }

    public CanvasModelTransform Transform { get; } = new();
    public ObservableCollection<PipeEdgeItem> Edges { get; }
    public ObservableCollection<PipeNodeItem> Nodes { get; }
    public ObservableCollection<InlineFittingItem> InlineFittings { get; }
    public ObservableCollection<string> FittingTypes { get; }
    public ICommand AddFittingCommand { get; }
    public ICommand RemoveFittingCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand CancelDrawingCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand CreateModelCommand { get; }
    public Action? CloseAction { get; set; }
    public Action<PipeNetworkDefinition>? CreateModelAction { get; set; }
    public double Elevation { get => _elevation; set { if (SetProperty(ref _elevation, value)) _network.Elevation = value; } }
    public double DiameterMm { get => _diameterMm; set => SetProperty(ref _diameterMm, value); }
    public PipeOutputMode OutputMode { get => _outputMode; set => SetProperty(ref _outputMode, value); }
    public Array OutputModes { get; } = Enum.GetValues(typeof(PipeOutputMode));
    public bool UseAngleSnap { get => _useAngleSnap; set => SetProperty(ref _useAngleSnap, value); }
    public bool IsPreviewVisible { get => _isPreviewVisible; private set => SetProperty(ref _isPreviewVisible, value); }
    public double PreviewX1 { get => _previewX1; private set => SetProperty(ref _previewX1, value); }
    public double PreviewY1 { get => _previewY1; private set => SetProperty(ref _previewY1, value); }
    public double PreviewX2 { get => _previewX2; private set => SetProperty(ref _previewX2, value); }
    public double PreviewY2 { get => _previewY2; private set => SetProperty(ref _previewY2, value); }

    public PipeEdgeItem? SelectedEdge
    {
        get => _selectedEdge;
        private set { if (SetProperty(ref _selectedEdge, value)) RefreshFittings(); }
    }
    public string SelectedFittingType { get => _selectedFittingType; set => SetProperty(ref _selectedFittingType, value); }
    public string Status { get => _status; private set => SetProperty(ref _status, value); }

    public void HandleCanvasClick(Point point)
    {
        if (_segmentStart is null)
        {
            _segmentStart = point;
            PreviewX1 = PreviewX2 = point.X; PreviewY1 = PreviewY2 = point.Y; IsPreviewVisible = true;
            Status = "끝점을 클릭하면 배관이 확정됩니다.";
            return;
        }
        var start = Transform.ToModel(_segmentStart.Value);
        var raw = Transform.ToModel(point);
        var snapped = _network.SnapPoint(raw);
        var end = raw.DistanceTo(snapped) > 0.001 ? snapped : Constrain(start, raw);
        _network.AddSegment(start, end);
        _segmentStart = null; IsPreviewVisible = false;
        RefreshGraph();
        Status = "기존 선 중간을 클릭하면 T 접점으로 연결됩니다.";
    }

    public void HandleCanvasMove(Point point)
    {
        if (_segmentStart is null) return;
        var start = Transform.ToModel(_segmentStart.Value); var raw = Transform.ToModel(point); var snapped = _network.SnapPoint(raw);
        var end = raw.DistanceTo(snapped) > 0.001 ? snapped : Constrain(start, raw);
        var screen = Transform.ToScreen(end);
        PreviewX1 = _segmentStart.Value.X; PreviewY1 = _segmentStart.Value.Y; PreviewX2 = screen.X; PreviewY2 = screen.Y; IsPreviewVisible = true;
    }

    private DHBIMWATER.Core.Geometry.Point2D Constrain(DHBIMWATER.Core.Geometry.Point2D start, DHBIMWATER.Core.Geometry.Point2D end)
    {
        if (!UseAngleSnap) return end;
        var dx = end.X - start.X; var dy = end.Y - start.Y; var length = Math.Sqrt(dx * dx + dy * dy);
        if (length < double.Epsilon) return end;
        var angle = Math.Round(Math.Atan2(dy, dx) / (Math.PI / 4)) * Math.PI / 4;
        return new(start.X + length * Math.Cos(angle), start.Y + length * Math.Sin(angle));
    }
    public void SelectEdge(Guid edgeId) => SelectedEdge = Edges.FirstOrDefault(x => x.Id == edgeId);

    private void AddFitting()
    {
        if (SelectedEdge is null) return;
        _network.AddInlineFitting(SelectedEdge.Id, SelectedFittingType);
        RefreshGraph();
        SelectedEdge = Edges.FirstOrDefault(x => x.Id == SelectedEdge.Id);
    }

    private void RemoveFitting(InlineFittingItem? fitting)
    {
        if (fitting is null || SelectedEdge is null) return;
        // TODO: 분할 후에도 엣지 ID를 보존하는 Phase 2 편집 API를 추가할 수 있다.
        var edge = _network.Edges.First(x => x.Id == SelectedEdge.Id);
        var retained = edge.InlineFittings.Where(x => x.Id != fitting.Id).Select((x, i) => x with { Order = i }).ToList();
        _network.RemoveInlineFitting(edge.Id, fitting.Id);
        RefreshGraph();
        SelectedEdge = Edges.FirstOrDefault(x => x.StartNodeId == edge.StartNodeId && x.EndNodeId == edge.EndNodeId);
    }

    private void CreateModel()
    {
        CreateModelAction?.Invoke(_network.ToDefinition(DiameterMm, OutputMode));
        Status = "Revit 모델 생성을 완료했습니다.";
    }

    private void Clear() { _network.Clear(); SelectedEdge = null; RefreshGraph(); Status = "그래프를 초기화했습니다."; }
    private void CancelDrawing() { _segmentStart = null; IsPreviewVisible = false; Status = "그리기를 취소했습니다."; }

    private void RefreshGraph()
    {
        Edges.Clear(); Nodes.Clear();
        foreach (var edge in _network.Edges)
        {
            var start = _network.Nodes.First(x => x.Id == edge.StartNodeId);
            var end = _network.Nodes.First(x => x.Id == edge.EndNodeId);
            var a = Transform.ToScreen(start.Position); var b = Transform.ToScreen(end.Position);
            Edges.Add(new PipeEdgeItem(edge.Id, edge.StartNodeId, edge.EndNodeId, a.X, a.Y, b.X, b.Y));
        }
        foreach (var node in _network.Nodes)
        {
            var p = Transform.ToScreen(node.Position);
            Nodes.Add(new PipeNodeItem(node.Id, p.X, p.Y, node.Degree, node.NodeKind));
        }
        RefreshFittings();
    }

    private void RefreshFittings()
    {
        InlineFittings.Clear();
        if (SelectedEdge is null) return;
        var edge = _network.Edges.FirstOrDefault(x => x.Id == SelectedEdge.Id);
        var display = Edges.FirstOrDefault(x => x.Id == SelectedEdge.Id);
        if (edge is null || display is null) return;
        foreach (var fitting in edge.InlineFittings.OrderBy(x => x.Order))
            InlineFittings.Add(new InlineFittingItem(fitting.Id, fitting.TypeKey, fitting.T, fitting.Order, display.X1 + (display.X2 - display.X1) * fitting.T, display.Y1 + (display.Y2 - display.Y1) * fitting.T));
    }
}

public sealed record PipeEdgeItem(Guid Id, Guid StartNodeId, Guid EndNodeId, double X1, double Y1, double X2, double Y2);
public sealed record PipeNodeItem(Guid Id, double X, double Y, int Degree, NodeKind NodeKind)
{
    public string Label => $"{NodeKind} ({Degree})";
    public string Color => NodeKind switch { NodeKind.Cap => "#7F8C8D", NodeKind.Inline => "#3498DB", NodeKind.Elbow => "#F39C12", NodeKind.Tee => "#9B59B6", _ => "#E74C3C" };
}
public sealed record InlineFittingItem(Guid Id, string TypeKey, double T, int Order, double X, double Y);
