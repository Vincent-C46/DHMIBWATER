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
    private double _selectedFittingT = 0.5;
    private string _selectedFittingType = "밸브";
    private string _status = "캔버스를 클릭하여 배관 시작점을 지정하세요.";
    private double _elevation;
    private double _referenceX, _referenceY;
    private double _diameterMm = 100;
    private PipeOutputMode _outputMode = PipeOutputMode.MepPipe;
    private bool _useAngleSnap = true, _isPreviewVisible, _snapReferencePoint = true, _snapEndpoint = true, _snapMidpoint, _snapQuadrant, _snapIntersection = true, _snapNearest = true, _isSnapMarkerVisible;
    private double _previewX1, _previewY1, _previewX2, _previewY2, _previewLengthX, _previewLengthY, _snapMarkerX, _snapMarkerY;
    private string _previewLength = "0 mm";
    private string _snapMarkerSymbol = "□";

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
    public ICommand DeleteSelectedEdgeCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand CreateModelCommand { get; }
    public Action? CloseAction { get; set; }
    public Action<PipeNetworkDefinition>? CreateModelAction { get; set; }
    public double Elevation { get => _elevation; set { if (SetProperty(ref _elevation, value)) _network.Elevation = value; } }
    public double ReferenceX { get => _referenceX; set => SetProperty(ref _referenceX, value); }
    public double ReferenceY { get => _referenceY; set => SetProperty(ref _referenceY, value); }
    public double ReferenceScreenX => Transform.PanOrigin.X;
    public double ReferenceScreenY => Transform.PanOrigin.Y;
    public double DiameterMm { get => _diameterMm; set => SetProperty(ref _diameterMm, value); }
    public PipeOutputMode OutputMode { get => _outputMode; set => SetProperty(ref _outputMode, value); }
    public Array OutputModes { get; } = Enum.GetValues(typeof(PipeOutputMode));
    public bool UseAngleSnap { get => _useAngleSnap; set => SetProperty(ref _useAngleSnap, value); }
    public bool SnapReferencePoint { get => _snapReferencePoint; set => SetProperty(ref _snapReferencePoint, value); }
    public bool SnapEndpoint { get => _snapEndpoint; set => SetProperty(ref _snapEndpoint, value); }
    public bool SnapMidpoint { get => _snapMidpoint; set => SetProperty(ref _snapMidpoint, value); }
    public bool SnapQuadrant { get => _snapQuadrant; set => SetProperty(ref _snapQuadrant, value); }
    public bool SnapIntersection { get => _snapIntersection; set => SetProperty(ref _snapIntersection, value); }
    public bool SnapNearest { get => _snapNearest; set => SetProperty(ref _snapNearest, value); }
    public bool IsPreviewVisible { get => _isPreviewVisible; private set => SetProperty(ref _isPreviewVisible, value); }
    public bool IsDrawing => _segmentStart is not null;
    public bool IsSnapMarkerVisible { get => _isSnapMarkerVisible; private set => SetProperty(ref _isSnapMarkerVisible, value); }
    public double SnapMarkerX { get => _snapMarkerX; private set => SetProperty(ref _snapMarkerX, value); }
    public double SnapMarkerY { get => _snapMarkerY; private set => SetProperty(ref _snapMarkerY, value); }
    public string SnapMarkerSymbol { get => _snapMarkerSymbol; private set => SetProperty(ref _snapMarkerSymbol, value); }
    public double PreviewX1 { get => _previewX1; private set => SetProperty(ref _previewX1, value); }
    public double PreviewY1 { get => _previewY1; private set => SetProperty(ref _previewY1, value); }
    public double PreviewX2 { get => _previewX2; private set => SetProperty(ref _previewX2, value); }
    public double PreviewY2 { get => _previewY2; private set => SetProperty(ref _previewY2, value); }
    public double PreviewLengthX { get => _previewLengthX; private set => SetProperty(ref _previewLengthX, value); }
    public double PreviewLengthY { get => _previewLengthY; private set => SetProperty(ref _previewLengthY, value); }
    public string PreviewLength { get => _previewLength; private set => SetProperty(ref _previewLength, value); }

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
            var rawStart = Transform.ToModel(point);
            var snappedStart = FindSnapPoint(rawStart) ?? rawStart;
            _segmentStart = Transform.ToScreen(snappedStart);
            PreviewX1 = PreviewX2 = _segmentStart.Value.X; PreviewY1 = PreviewY2 = _segmentStart.Value.Y;
            PreviewLengthX = _segmentStart.Value.X; PreviewLengthY = _segmentStart.Value.Y; PreviewLength = "0.000 m"; IsPreviewVisible = true;
            Status = "끝점을 클릭하면 배관이 확정됩니다.";
            return;
        }
        var start = Transform.ToModel(_segmentStart.Value);
        var raw = Transform.ToModel(point);
        var snapped = FindSnapPoint(raw);
        var end = snapped ?? Constrain(start, raw);
        _network.AddSegment(start, end);
        _segmentStart = null; IsPreviewVisible = false; IsSnapMarkerVisible = false;
        RefreshGraph();
        Status = "기존 선 중간을 클릭하면 T 접점으로 연결됩니다.";
    }

    public void HandleCanvasMove(Point point)
    {
        var raw = Transform.ToModel(point); var snapped = FindSnapPoint(raw);
        if (snapped is not null)
        {
            var marker = Transform.ToScreen(snapped);
            SnapMarkerX = marker.X; SnapMarkerY = marker.Y; SnapMarkerSymbol = GetSnapMarkerSymbol(raw, snapped); IsSnapMarkerVisible = true;
        }
        else IsSnapMarkerVisible = false;
        if (_segmentStart is null) return;
        var start = Transform.ToModel(_segmentStart.Value);
        var end = Constrain(start, raw);
        var screen = Transform.ToScreen(end);
        PreviewX1 = _segmentStart.Value.X; PreviewY1 = _segmentStart.Value.Y; PreviewX2 = screen.X; PreviewY2 = screen.Y;
        PreviewLengthX = (PreviewX1 + PreviewX2) / 2; PreviewLengthY = (PreviewY1 + PreviewY2) / 2; PreviewLength = $"{start.DistanceTo(end) / 1000:N3} m"; IsPreviewVisible = true;
    }

    private DHBIMWATER.Core.Geometry.Point2D Constrain(DHBIMWATER.Core.Geometry.Point2D start, DHBIMWATER.Core.Geometry.Point2D end)
    {
        if (!UseAngleSnap) return end;
        var dx = end.X - start.X; var dy = end.Y - start.Y; var length = Math.Sqrt(dx * dx + dy * dy);
        if (length < double.Epsilon) return end;
        var angle = Math.Round(Math.Atan2(dy, dx) / (Math.PI / 4)) * Math.PI / 4;
        return new(start.X + length * Math.Cos(angle), start.Y + length * Math.Sin(angle));
    }
    public void HandleCanvasSizeChanged(double width, double height)
    {
        if (width <= 0 || height <= 0) return;
        Transform.PanOrigin = new Point(width / 2, height / 2);
        OnPropertyChanged(nameof(ReferenceScreenX));
        OnPropertyChanged(nameof(ReferenceScreenY));
        RefreshGraph();
    }

    private DHBIMWATER.Core.Geometry.Point2D? FindSnapPoint(DHBIMWATER.Core.Geometry.Point2D point)
    {
        var mode = PipeSnapMode.None;
        if (SnapEndpoint) mode |= PipeSnapMode.Endpoint;
        if (SnapMidpoint) mode |= PipeSnapMode.Midpoint;
        if (SnapQuadrant) mode |= PipeSnapMode.Quadrant;
        if (SnapIntersection) mode |= PipeSnapMode.Intersection;
        if (SnapNearest) mode |= PipeSnapMode.Nearest;
        var networkSnap = _network.FindSnapPoint(point, mode);
        var referencePoint = new DHBIMWATER.Core.Geometry.Point2D(0, 0);
        var referenceSnap = SnapReferencePoint && point.DistanceTo(referencePoint) <= PipeTopologyBuilder.SnapTolerance ? referencePoint : null;
        if (networkSnap is null) return referenceSnap;
        if (referenceSnap is null) return networkSnap;
        return point.DistanceTo(networkSnap) <= point.DistanceTo(referenceSnap) ? networkSnap : referenceSnap;
    }
    public void SelectEdge(Guid edgeId, Point? position = null)
    {
        SelectedEdge = Edges.FirstOrDefault(x => x.Id == edgeId);
        if (SelectedEdge is not null && position is not null)
        {
            var dx = SelectedEdge.X2 - SelectedEdge.X1; var dy = SelectedEdge.Y2 - SelectedEdge.Y1;
            var lengthSquared = dx * dx + dy * dy;
            _selectedFittingT = lengthSquared <= double.Epsilon ? 0.5 : Math.Clamp(((position.Value.X - SelectedEdge.X1) * dx + (position.Value.Y - SelectedEdge.Y1) * dy) / lengthSquared, 0, 1);
        }
        RefreshGraph();
    }

    public void HandleEscape()
    {
        if (IsDrawing) { CancelDrawing(); return; }
        if (SelectedEdge is null) return;
        SelectedEdge = null;
        RefreshGraph();
        Status = "선택을 취소했습니다.";
    }

    public void DeleteSelectedEdge()
    {
        if (SelectedEdge is null) return;
        _network.RemoveSegment(SelectedEdge.Id);
        SelectedEdge = null;
        RefreshGraph();
        Status = "선택한 배관을 삭제했습니다.";
    }

    private void AddFitting()
    {
        if (SelectedEdge is null) return;
        _network.AddInlineFitting(SelectedEdge.Id, SelectedFittingType, _selectedFittingT);
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
        CreateModelAction?.Invoke(_network.ToDefinition(DiameterMm, OutputMode, new(ReferenceX, ReferenceY)));
        Status = "Revit 모델 생성 요청을 전송했습니다.";
    }

    private void Clear() { _network.Clear(); SelectedEdge = null; RefreshGraph(); Status = "그래프를 초기화했습니다."; }
    private void CancelDrawing() { _segmentStart = null; IsPreviewVisible = false; IsSnapMarkerVisible = false; Status = "그리기를 취소했습니다."; }

    private string GetSnapMarkerSymbol(DHBIMWATER.Core.Geometry.Point2D raw, DHBIMWATER.Core.Geometry.Point2D snapped)
    {
        var referencePoint = new DHBIMWATER.Core.Geometry.Point2D(0, 0);
        if (SnapReferencePoint && snapped.DistanceTo(referencePoint) <= double.Epsilon) return "+";
        if (SnapIntersection && _network.Nodes.Any(x => x.Degree >= 3 && x.Position.DistanceTo(snapped) <= double.Epsilon)) return "×";
        if (SnapEndpoint && _network.Nodes.Any(x => x.Position.DistanceTo(snapped) <= double.Epsilon)) return "□";
        foreach (var edge in _network.Edges)
        {
            if (SnapMidpoint && IsAt(edge, snapped, 0.5)) return "△";
            if (SnapQuadrant && (IsAt(edge, snapped, 0.25) || IsAt(edge, snapped, 0.75))) return "◇";
        }
        return "·";
    }

    private bool IsAt(PipeEdge edge, DHBIMWATER.Core.Geometry.Point2D point, double t)
    {
        var start = _network.Nodes.First(x => x.Id == edge.StartNodeId).Position;
        var end = _network.Nodes.First(x => x.Id == edge.EndNodeId).Position;
        return point.DistanceTo(new(start.X + (end.X - start.X) * t, start.Y + (end.Y - start.Y) * t)) <= double.Epsilon;
    }

    private void RefreshGraph()
    {
        Edges.Clear(); Nodes.Clear();
        foreach (var edge in _network.Edges)
        {
            var start = _network.Nodes.First(x => x.Id == edge.StartNodeId);
            var end = _network.Nodes.First(x => x.Id == edge.EndNodeId);
            var a = Transform.ToScreen(start.Position); var b = Transform.ToScreen(end.Position);
            Edges.Add(new PipeEdgeItem(edge.Id, edge.StartNodeId, edge.EndNodeId, a.X, a.Y, b.X, b.Y, start.Position.DistanceTo(end.Position), edge.Id == SelectedEdge?.Id));
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

public sealed record PipeEdgeItem(Guid Id, Guid StartNodeId, Guid EndNodeId, double X1, double Y1, double X2, double Y2, double LengthMm, bool IsSelected)
{
    public double LengthX => (X1 + X2) / 2;
    public double LengthY => (Y1 + Y2) / 2;
    public string LengthLabel => $"{LengthMm / 1000:N3} m";
    public string Stroke => IsSelected ? "#E67E22" : "#2878B8";
}
public sealed record PipeNodeItem(Guid Id, double X, double Y, int Degree, NodeKind NodeKind)
{
    public string Label => $"{NodeKind} ({Degree})";
    public string Color => NodeKind switch { NodeKind.Cap => "#7F8C8D", NodeKind.Inline => "#3498DB", NodeKind.Elbow => "#F39C12", NodeKind.Tee => "#9B59B6", _ => "#E74C3C" };
}
public sealed record InlineFittingItem(Guid Id, string TypeKey, double T, int Order, double X, double Y);
