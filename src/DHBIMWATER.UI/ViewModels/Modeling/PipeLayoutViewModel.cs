using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Piping;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;
using DHBIMWATER.UI.Utilities;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace DHBIMWATER.UI.ViewModels.Modeling;

public enum PipeLayoutMode { Selection, Drawing }

public sealed class PipeLayoutViewModel : ViewModelBase
{
    private readonly IElementTypeQueryRepo _typeRepo;
    private PipeNetwork _network = new();
    private readonly Stack<PipeNetwork> _undoHistory = new();
    private Point? _segmentStart;
    private PipeEdgeItem? _selectedEdge;
    private PipeLayoutMode _mode = PipeLayoutMode.Drawing;
    private InlineFittingItem? _selectedFitting;
    private string? _selectedFamilyTypeName;
    private double _gridSnapMm = 1;
    private Guid? _fittingPreviewEdgeId;
    private double _fittingPreviewT;
    private bool _isFittingPreviewVisible;
    private double _fittingPreviewX, _fittingPreviewY, _fittingDimX1, _fittingDimY1, _fittingDimX2, _fittingDimY2;
    private string _fittingDimLabel = "0";
    private string _status = "캔버스를 클릭하여 배관 시작점을 지정하세요.";
    private double _elevation;
    private double _referenceX, _referenceY;
    private double _diameterMm = 100;
    private string? _selectedLevelName;
    private string? _segmentFamilyTypeName, _lengthParameterName, _diameterParameterName;
    private double _straightLengthMm = PipeSegmentPlan.StraightLengthMm;
    private string? _bendFamilyTypeName, _teeFamilyTypeName;
    /// <summary>관 길이 파라미터를 사용자가 직접 골랐는지. true면 관 패밀리를 바꿔도 자동 추정으로 덮어쓰지 않는다.</summary>
    private bool _lengthParameterPinned;
    private bool _diameterParameterPinned;
    private bool _isCreating;
    private int _inputValidationErrorCount;
    private bool _useAngleSnap = true, _isPreviewVisible, _snapReferencePoint = true, _snapEndpoint = true, _snapMidpoint, _snapQuadrant, _snapIntersection = true, _snapNearest = true, _isSnapMarkerVisible, _isCursorVisible;
    private double _previewX1, _previewY1, _previewX2, _previewY2, _previewLengthX, _previewLengthY, _snapMarkerX, _snapMarkerY, _cursorX, _cursorY;
    private string _previewLength = "0 mm";
    private string _previewAngle = "0.0°";
    private string _snapMarkerSymbol = "□";
    private string _snapMarkerLabel = "끝점";

    /// <summary>Revit 프로젝트 좌표(mm) 기준 원본 외곽. 캔버스 좌표는 기준점을 뺀 <see cref="_canvasOutline"/>이다.</summary>
    private ValveRoomOutline _outline = ValveRoomOutline.Empty;
    private ValveRoomOutline _canvasOutline = ValveRoomOutline.Empty;
    private double _canvasWidth, _canvasHeight;
    private bool _hasCanvasSize;
    private double _inOffsetMm = 500, _outOffsetMm = 500;
    /// <summary>사용자가 IN/OUT 이격을 직접 입력해 바꿨는지. true면 외곽 재피킹 시 자동값으로 덮어쓰지 않는다.</summary>
    private bool _inOffsetUserEdited, _outOffsetUserEdited;
    /// <summary>IN/OUT 화살표가 벽 내측면에 닿는 접점(캔버스 좌표, mm). 끝점 스냅 후보로 쓴다.</summary>
    private readonly List<DHBIMWATER.Core.Geometry.Point2D> _arrowAnchors = [];
    private bool _arrowFromTop;
    private bool _snapOutline = true;

    public PipeLayoutViewModel(IElementTypeQueryRepo typeRepo)
    {
        _typeRepo = typeRepo;
        FamilyTypeNames = [];
        SegmentFamilyTypeNames = [];
        LengthParameterNames = [];
        Edges = [];
        Nodes = [];
        InlineFittings = [];
        OutlineWalls = [];
        OutlineArrows = [];
        TempDimensions = [];
        LevelNames = new(_typeRepo.GetLevelNames());
        _selectedLevelName = LevelNames.FirstOrDefault();
        PickOutlineCommand = new RelayCommand(_ => PickOutline(), _ => PickOutlineAction is not null);
        ClearOutlineCommand = new RelayCommand(_ => ClearOutline(), _ => HasOutline);
        RemoveFittingCommand = new RelayCommand(x => RemoveFitting(x as InlineFittingItem));
        ClearCommand = new RelayCommand(_ => Clear());
        CancelDrawingCommand = new RelayCommand(_ => CancelDrawing(), _ => IsDrawing);
        UndoCommand = new RelayCommand(_ => Undo(), _ => CanUndo);
        DeleteSelectedEdgeCommand = new RelayCommand(_ => DeleteSelectedEdge(), _ => SelectedEdge is not null);
        SelectModeCommand = new RelayCommand(_ => Mode = PipeLayoutMode.Selection);
        DrawModeCommand = new RelayCommand(_ => Mode = PipeLayoutMode.Drawing);
        DeleteSelectedFittingCommand = new RelayCommand(_ => DeleteSelectedFitting(), _ => SelectedFitting is not null);
        CloseCommand = new RelayCommand(_ => CloseAction?.Invoke());
        CreateModelCommand = new RelayCommand(_ => CreateModel(), _ => CanCreateModel());
        RefreshFamilyTypeNames();
    }

    public CanvasModelTransform Transform { get; } = new();
    public ObservableCollection<PipeEdgeItem> Edges { get; }
    public ObservableCollection<PipeNodeItem> Nodes { get; }
    public ObservableCollection<InlineFittingItem> InlineFittings { get; }
    /// <summary>선택된 카테고리(배관 밸브류/일반모델)에 로드된 패밀리 목록. "패밀리명 : 타입명" 형식.</summary>
    public ObservableCollection<string> FamilyTypeNames { get; }
    /// <summary>관·곡관·T형 콤보의 공용 배관 부속류 목록.</summary>
    public ObservableCollection<string> SegmentFamilyTypeNames { get; }
    /// <summary>선택된 관 패밀리의 인스턴스 파라미터 후보.</summary>
    public ObservableCollection<string> LengthParameterNames { get; }
    public ObservableCollection<OutlineWallItem> OutlineWalls { get; }
    public ObservableCollection<OutlineArrowItem> OutlineArrows { get; }
    public ObservableCollection<TempDimensionItem> TempDimensions { get; }
    public ObservableCollection<string> LevelNames { get; }
    public ICommand PickOutlineCommand { get; }
    public ICommand ClearOutlineCommand { get; }
    public ICommand RemoveFittingCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand CancelDrawingCommand { get; }
    public ICommand UndoCommand { get; }
    public ICommand DeleteSelectedEdgeCommand { get; }
    public ICommand SelectModeCommand { get; }
    public ICommand DrawModeCommand { get; }
    public ICommand DeleteSelectedFittingCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand CreateModelCommand { get; }
    public Action? CloseAction { get; set; }
    public Action<PipeNetworkDefinition>? CreateModelAction { get; set; }
    /// <summary>Revit 뷰에서 외곽 벽체 피킹을 시작한다. 결과는 <see cref="ApplyOutline"/>으로 되돌아온다.</summary>
    public Action? PickOutlineAction { get; set; }
    public double Elevation { get => _elevation; set { if (SetProperty(ref _elevation, value)) { _network.Elevation = value; NotifyValidationChanged(); } } }
    public double ReferenceX { get => _referenceX; set { if (SetProperty(ref _referenceX, value)) { RefreshOutline(); NotifyValidationChanged(); } } }
    public double ReferenceY { get => _referenceY; set { if (SetProperty(ref _referenceY, value)) { RefreshOutline(); NotifyValidationChanged(); } } }

    public bool HasOutline => !_outline.IsEmpty;
    /// <summary>IN 화살표의 기준 벽으로부터의 상하 이격(mm).</summary>
    public double InOffsetMm { get => _inOffsetMm; set { if (SetProperty(ref _inOffsetMm, value)) { _inOffsetUserEdited = true; RefreshOutline(); NotifyValidationChanged(); } } }
    /// <summary>OUT 화살표의 기준 벽으로부터의 상하 이격(mm).</summary>
    public double OutOffsetMm { get => _outOffsetMm; set { if (SetProperty(ref _outOffsetMm, value)) { _outOffsetUserEdited = true; RefreshOutline(); NotifyValidationChanged(); } } }
    /// <summary>true면 상단 벽 내측면, false면 하단 벽 내측면에서 이격을 잰다.</summary>
    public bool ArrowFromTop { get => _arrowFromTop; set { if (SetProperty(ref _arrowFromTop, value)) RefreshOutline(); } }
    /// <summary>외곽선 변에도 OSNAP(끝점·중간점·근처점)을 적용할지 여부.</summary>
    public bool SnapOutline { get => _snapOutline; set => SetProperty(ref _snapOutline, value); }
    public double ReferenceScreenX => Transform.PanOrigin.X;
    public double ReferenceScreenY => Transform.PanOrigin.Y;
    public double DiameterMm { get => _diameterMm; set { if (!double.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value), "관경은 0보다 큰 숫자여야 합니다."); if (SetProperty(ref _diameterMm, value)) NotifyValidationChanged(); } }
    public string? SelectedLevelName { get => _selectedLevelName; set { if (SetProperty(ref _selectedLevelName, value)) NotifyValidationChanged(); } }
    /// <summary>직관·단관을 모두 만드는 단일 관 패밀리. 길이는 <see cref="LengthParameterName"/>으로 구동한다.</summary>
    public string? SegmentFamilyTypeName
    {
        get => _segmentFamilyTypeName;
        set
        {
            if (!SetProperty(ref _segmentFamilyTypeName, value)) return;
            RefreshSegmentParameterNames();
            NotifyValidationChanged();
        }
    }
    public string? LengthParameterName
    {
        get => _lengthParameterName;
        set
        {
            if (!SetProperty(ref _lengthParameterName, value)) return;
            _lengthParameterPinned = !string.IsNullOrWhiteSpace(value);
            NotifyValidationChanged();
        }
    }
    /// <summary>관 인스턴스에 직경(mm)을 기록할 파라미터명. 비워 두면 직경을 구동하지 않는다.</summary>
    public string? DiameterParameterName
    {
        get => _diameterParameterName;
        set
        {
            if (!SetProperty(ref _diameterParameterName, value)) return;
            _diameterParameterPinned = !string.IsNullOrWhiteSpace(value);
            NotifyValidationChanged();
        }
    }
    public string? BendFamilyTypeName { get => _bendFamilyTypeName; set { if (SetProperty(ref _bendFamilyTypeName, value)) NotifyValidationChanged(); } }
    public string? TeeFamilyTypeName { get => _teeFamilyTypeName; set { if (SetProperty(ref _teeFamilyTypeName, value)) NotifyValidationChanged(); } }
    /// <summary>직관 1본의 정척 길이(mm). 관 패밀리 수식의 직관/단관 분기 기준과 같아야 한다.</summary>
    public double StraightLengthMm
    {
        get => _straightLengthMm;
        set
        {
            if (!double.IsFinite(value) || value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), "정척 길이는 0보다 큰 숫자여야 합니다.");
            if (SetProperty(ref _straightLengthMm, value)) NotifyValidationChanged();
        }
    }

    public bool IsCreating { get => _isCreating; private set { if (SetProperty(ref _isCreating, value)) NotifyValidationChanged(); } }
    public bool CanUndo => _undoHistory.Count > 0;
    public string InteractionModeText => IsCreating ? "모델 생성 중" : IsPlacingFitting ? "부속 배치" : IsDrawing ? "선 그리기" : SelectedFitting is not null ? "부속 선택" : SelectedEdge is not null ? "선 선택" : IsSelectionMode ? "선택 대기" : "그리기 대기";
    public string NetworkSummary => $"배관 {_network.Edges.Count} · Tee {_network.Nodes.Count(x => x.NodeKind == NodeKind.Tee)} · 부속 {_network.Edges.Sum(x => x.InlineFittings.Count)}";
    public string ValidationSummary => string.Join("  ·  ", GetValidationErrors());
    /// <summary>선 그리기·부속 배치 시 치수가 반올림되는 격자 단위(mm). 프리셋 외의 양수도 직접 입력할 수 있다.</summary>
    public double[] GridSnapOptions { get; } = [1, 10, 100];
    public double GridSnapMm
    {
        get => _gridSnapMm;
        set
        {
            if (!double.IsFinite(value) || value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), "격자 스냅 단위는 0보다 큰 숫자여야 합니다.");
            if (SetProperty(ref _gridSnapMm, value)) NotifyValidationChanged();
        }
    }
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
    public string SnapMarkerLabel { get => _snapMarkerLabel; private set => SetProperty(ref _snapMarkerLabel, value); }
    public double CursorX { get => _cursorX; private set => SetProperty(ref _cursorX, value); }
    public double CursorY { get => _cursorY; private set => SetProperty(ref _cursorY, value); }
    public bool IsCursorVisible { get => _isCursorVisible; private set => SetProperty(ref _isCursorVisible, value); }
    public double PreviewX1 { get => _previewX1; private set => SetProperty(ref _previewX1, value); }
    public double PreviewY1 { get => _previewY1; private set => SetProperty(ref _previewY1, value); }
    public double PreviewX2 { get => _previewX2; private set => SetProperty(ref _previewX2, value); }
    public double PreviewY2 { get => _previewY2; private set => SetProperty(ref _previewY2, value); }
    public double PreviewLengthX { get => _previewLengthX; private set => SetProperty(ref _previewLengthX, value); }
    public double PreviewLengthY { get => _previewLengthY; private set => SetProperty(ref _previewLengthY, value); }
    public string PreviewLength { get => _previewLength; private set => SetProperty(ref _previewLength, value); }
    public string PreviewAngle { get => _previewAngle; private set => SetProperty(ref _previewAngle, value); }

    public PipeEdgeItem? SelectedEdge
    {
        get => _selectedEdge;
        private set { if (SetProperty(ref _selectedEdge, value)) { RefreshFittings(); OnPropertyChanged(nameof(InteractionModeText)); CommandManager.InvalidateRequerySuggested(); } }
    }
    public PipeLayoutMode Mode
    {
        get => _mode;
        set
        {
            if (_mode == value) return;
            if (value != PipeLayoutMode.Drawing && IsDrawing) CancelDrawing();
            if (value != PipeLayoutMode.Selection) { SelectedEdge = null; SelectedFitting = null; }
            _mode = value;
            RefreshGraph();
            OnPropertyChanged(nameof(Mode));
            OnPropertyChanged(nameof(IsSelectionMode));
            OnPropertyChanged(nameof(IsDrawMode));
            OnPropertyChanged(nameof(IsPlacingFitting));
            OnPropertyChanged(nameof(InteractionModeText));
            CommandManager.InvalidateRequerySuggested();
        }
    }
    public bool IsSelectionMode => Mode == PipeLayoutMode.Selection;
    public bool IsDrawMode => Mode == PipeLayoutMode.Drawing;

    public InlineFittingItem? SelectedFitting
    {
        get => _selectedFitting;
        private set { if (SetProperty(ref _selectedFitting, value)) { RefreshFittings(); OnPropertyChanged(nameof(InteractionModeText)); CommandManager.InvalidateRequerySuggested(); } }
    }
    /// <summary>배치할 배관부속 FamilySymbol("패밀리명 : 타입명"). 리스트에서 고르면 배치 모드로 들어간다(카탈로그 매핑 없음).</summary>
    public string? SelectedFamilyTypeName
    {
        get => _selectedFamilyTypeName;
        set
        {
            if (!SetProperty(ref _selectedFamilyTypeName, value)) return;
            OnPropertyChanged(nameof(IsPlacingFitting));
            OnPropertyChanged(nameof(InteractionModeText));
            if (string.IsNullOrWhiteSpace(value)) HideFittingPreview();
        }
    }
    /// <summary>true면 리스트에서 패밀리를 선택한 상태 — 캔버스 클릭이 선 그리기가 아니라 부속 배치로 동작한다.</summary>
    public bool IsPlacingFitting => IsDrawMode && !string.IsNullOrWhiteSpace(SelectedFamilyTypeName);
    public bool IsFittingPreviewVisible { get => _isFittingPreviewVisible; private set => SetProperty(ref _isFittingPreviewVisible, value); }
    public double FittingPreviewX { get => _fittingPreviewX; private set => SetProperty(ref _fittingPreviewX, value); }
    public double FittingPreviewY { get => _fittingPreviewY; private set => SetProperty(ref _fittingPreviewY, value); }
    public double FittingDimX1 { get => _fittingDimX1; private set => SetProperty(ref _fittingDimX1, value); }
    public double FittingDimY1 { get => _fittingDimY1; private set => SetProperty(ref _fittingDimY1, value); }
    public double FittingDimX2 { get => _fittingDimX2; private set => SetProperty(ref _fittingDimX2, value); }
    public double FittingDimY2 { get => _fittingDimY2; private set => SetProperty(ref _fittingDimY2, value); }
    public string FittingDimLabel { get => _fittingDimLabel; private set => SetProperty(ref _fittingDimLabel, value); }
    public string Status { get => _status; private set => SetProperty(ref _status, value); }

    public void HandleCanvasClick(Point point)
    {
        if (!IsDrawMode) return;
        if (IsPlacingFitting) { PlacePreviewedFitting(); return; }
        if (_segmentStart is null)
        {
            var rawStart = Transform.ToModel(point);
            var snappedStart = FindSnapPoint(rawStart)?.Point ?? rawStart;
            var screenStart = Transform.ToScreen(snappedStart);
            SetSegmentStart(screenStart);
            PreviewX1 = PreviewX2 = screenStart.X; PreviewY1 = PreviewY2 = screenStart.Y;
            PreviewLengthX = screenStart.X; PreviewLengthY = screenStart.Y; PreviewLength = "0.000 m"; PreviewAngle = "0.0°"; IsPreviewVisible = true;
            Status = "끝점을 클릭하면 배관이 확정됩니다.";
            return;
        }
        var start = Transform.ToModel(_segmentStart.Value);
        var raw = Transform.ToModel(point);
        var (end, _) = ResolveDrawingEnd(start, raw);
        var before = _network.DeepClone();
        _network.AddSegment(start, end);
        RecordUndoIfChanged(before);
        SetSegmentStart(null); IsPreviewVisible = false; IsSnapMarkerVisible = false; TempDimensions.Clear();
        RefreshGraph();
        Status = "기존 선 중간을 클릭하면 T 접점으로 연결됩니다.";
    }

    public void HandleCanvasMove(Point point)
    {
        var raw = Transform.ToModel(point);
        var drawingStart = _segmentStart is null ? null : Transform.ToModel(_segmentStart.Value);
        var snapped = FindSnapPoint(raw, drawingStart);
        var cursor = Transform.ToScreen(snapped?.Point ?? raw);
        CursorX = cursor.X;
        CursorY = cursor.Y;
        IsCursorVisible = true;
        if (IsPlacingFitting && _segmentStart is null) { UpdateFittingPreview(raw); return; }
        HideFittingPreview();
        if (snapped is not null)
        {
            var marker = Transform.ToScreen(snapped.Value.Point);
            SnapMarkerX = marker.X;
            SnapMarkerY = marker.Y;
            var isOrigin = snapped.Value.Point.DistanceTo(new DHBIMWATER.Core.Geometry.Point2D(0, 0)) <= double.Epsilon;
            // 기준점은 Kind가 Intersection이지만 기존 CAD 표기 의미를 유지해 + / 기준점으로 덮어쓴다.
            SnapMarkerSymbol = isOrigin ? "+" : GetSnapMarkerSymbol(snapped.Value);
            SnapMarkerLabel = isOrigin ? "기준점" : GetSnapMarkerLabel(snapped.Value);
            IsSnapMarkerVisible = true;
        }
        else IsSnapMarkerVisible = false;

        if (drawingStart is null) { TempDimensions.Clear(); return; }
        var (end, clamped) = ResolveDrawingEnd(drawingStart, raw);
        UpdateTempDimensions(end);   // 임시치수는 그리는 중에만 표시한다
        var startScreen = Transform.ToScreen(drawingStart);
        var screen = Transform.ToScreen(end);
        PreviewX1 = startScreen.X; PreviewY1 = startScreen.Y; PreviewX2 = screen.X; PreviewY2 = screen.Y;
        PreviewLengthX = (PreviewX1 + PreviewX2) / 2; PreviewLengthY = (PreviewY1 + PreviewY2) / 2; PreviewLength = $"{drawingStart.DistanceTo(end) / 1000:N3} m";
        var angle = Math.Atan2(end.Y - drawingStart.Y, end.X - drawingStart.X) * 180.0 / Math.PI;
        if (angle < 0) angle += 360;
        PreviewAngle = $"{angle:N1}°";
        IsPreviewVisible = true;
        Status = clamped
            ? $"정척 길이 {StraightLengthMm:N0}mm를 넘을 수 없어 끝점을 제한했습니다."
            : "끝점을 클릭하면 배관이 확정됩니다.";
    }

    public void HandleCanvasLeave()
    {
        IsCursorVisible = false;
        IsSnapMarkerVisible = false;
    }

    /// <summary>작도 중 끝점을 구한다. 정척 길이(<see cref="StraightLengthMm"/>)를 넘는 OSNAP 후보는 버리고,
    /// 자유 좌표는 방향을 유지한 채 길이를 정척으로 클램프한다(docs/56).</summary>
    /// <returns>끝점과, 상한 때문에 잘렸는지 여부.</returns>
    private (DHBIMWATER.Core.Geometry.Point2D End, bool Clamped) ResolveDrawingEnd(
        DHBIMWATER.Core.Geometry.Point2D start,
        DHBIMWATER.Core.Geometry.Point2D raw)
    {
        const double tolerance = 1e-6;
        var snapped = FindSnapPoint(raw, start, out var snapFiltered);
        if (snapped is not null) return (snapped.Value.Point, snapFiltered);

        var constrained = Constrain(start, raw);
        var end = ClampToStraightLength(start, constrained);
        return (end, snapFiltered || constrained.DistanceTo(end) > tolerance);
    }

    /// <summary>start→end 길이가 정척을 넘으면 방향은 유지한 채 길이를 정척으로 줄인다.</summary>
    private DHBIMWATER.Core.Geometry.Point2D ClampToStraightLength(
        DHBIMWATER.Core.Geometry.Point2D start,
        DHBIMWATER.Core.Geometry.Point2D end)
    {
        if (!double.IsFinite(StraightLengthMm) || StraightLengthMm <= 0) return end;

        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length <= StraightLengthMm || length < double.Epsilon) return end;

        var scale = StraightLengthMm / length;
        return new(start.X + dx * scale, start.Y + dy * scale);
    }

    /// <summary>OSNAP으로 스냅되지 않은 자유 좌표를 <see cref="GridSnapMm"/> 격자에 맞춘다.
    /// 각도 스냅이 켜져 있으면 각도는 그대로 두고 길이만 반올림하고, 꺼져 있으면 X·Y를 각각 반올림한다.
    /// OSNAP 결과(끝점·중간점 등 기존 지오메트리에 정확히 붙는 점)에는 적용하지 않는다(호출부에서 snapped ?? Constrain(...) 순서로 보장).</summary>
    private DHBIMWATER.Core.Geometry.Point2D Constrain(DHBIMWATER.Core.Geometry.Point2D start, DHBIMWATER.Core.Geometry.Point2D end)
    {
        var dx = end.X - start.X; var dy = end.Y - start.Y; var length = Math.Sqrt(dx * dx + dy * dy);
        if (length < double.Epsilon) return end;
        if (!UseAngleSnap)
        {
            var x = start.X + Math.Round(dx / GridSnapMm) * GridSnapMm;
            var y = start.Y + Math.Round(dy / GridSnapMm) * GridSnapMm;
            return new(x, y);
        }
        var angle = Math.Round(Math.Atan2(dy, dx) / (Math.PI / 4)) * Math.PI / 4;
        var snappedLength = Math.Round(length / GridSnapMm) * GridSnapMm;
        return new(start.X + snappedLength * Math.Cos(angle), start.Y + snappedLength * Math.Sin(angle));
    }

    /// <summary>커서에 가장 가까운 엣지 위의 점을 찾고(허용오차 <see cref="PipeTopologyBuilder.SnapTolerance"/>),
    /// 엣지 시작점부터의 거리를 <see cref="GridSnapMm"/> 격자로 반올림해 배치 미리보기를 갱신한다.</summary>
    private void UpdateFittingPreview(DHBIMWATER.Core.Geometry.Point2D cursor)
    {
        var target = FindNearestEdgePoint(cursor);
        if (target is null) { HideFittingPreview(); return; }
        var (edge, t, point, startPosition) = target.Value;
        _fittingPreviewEdgeId = edge.Id; _fittingPreviewT = t;
        var screen = Transform.ToScreen(point);
        FittingPreviewX = screen.X; FittingPreviewY = screen.Y;
        IsFittingPreviewVisible = true;

        var dimStart = Transform.ToScreen(startPosition);
        FittingDimX1 = dimStart.X; FittingDimY1 = dimStart.Y; FittingDimX2 = screen.X; FittingDimY2 = screen.Y;
        FittingDimLabel = $"{point.DistanceTo(startPosition):N0} mm";
        Status = "클릭하면 이 위치에 부속을 배치합니다. Esc로 배치를 취소합니다.";
    }

    private void HideFittingPreview()
    {
        if (!IsFittingPreviewVisible) return;
        IsFittingPreviewVisible = false;
        _fittingPreviewEdgeId = null;
    }

    private (PipeEdge Edge, double T, DHBIMWATER.Core.Geometry.Point2D Point, DHBIMWATER.Core.Geometry.Point2D StartPosition)? FindNearestEdgePoint(DHBIMWATER.Core.Geometry.Point2D cursor)
    {
        PipeEdge? bestEdge = null; double bestT = 0, bestDistance = double.MaxValue;
        DHBIMWATER.Core.Geometry.Point2D? bestStart = null, bestEnd = null;
        foreach (var edge in _network.Edges)
        {
            var start = _network.Nodes.First(x => x.Id == edge.StartNodeId).Position;
            var end = _network.Nodes.First(x => x.Id == edge.EndNodeId).Position;
            var t = Math.Clamp(Segment2D.ParameterOnSegment(cursor, start, end), 0, 1);
            var point = new DHBIMWATER.Core.Geometry.Point2D(start.X + (end.X - start.X) * t, start.Y + (end.Y - start.Y) * t);
            var distance = point.DistanceTo(cursor);
            if (distance > PipeTopologyBuilder.SnapTolerance || distance >= bestDistance) continue;
            bestEdge = edge; bestT = t; bestDistance = distance; bestStart = start; bestEnd = end;
        }
        if (bestEdge is null) return null;

        var startPosition = bestStart!;
        var endPosition = bestEnd!;
        var length = startPosition.DistanceTo(endPosition);
        if (length < double.Epsilon) return (bestEdge, bestT, startPosition, startPosition);
        var distanceFromStart = length * bestT;
        var snappedDistance = Math.Clamp(Math.Round(distanceFromStart / GridSnapMm) * GridSnapMm, 0, length);
        var snappedT = snappedDistance / length;
        var snappedPoint = new DHBIMWATER.Core.Geometry.Point2D(startPosition.X + (endPosition.X - startPosition.X) * snappedT, startPosition.Y + (endPosition.Y - startPosition.Y) * snappedT);
        return (bestEdge, snappedT, snappedPoint, startPosition);
    }

    private void PlacePreviewedFitting()
    {
        if (_fittingPreviewEdgeId is null || string.IsNullOrWhiteSpace(SelectedFamilyTypeName)) return;
        const string category = "배관 밸브류";
        var before = _network.DeepClone();
        _network.AddInlineFitting(_fittingPreviewEdgeId.Value, category, SelectedFamilyTypeName, _fittingPreviewT);
        RecordUndoIfChanged(before);
        RefreshGraph();
        Status = "부속을 배치했습니다. 계속 배치하거나 Esc로 종료하세요.";
    }
    public void HandleCanvasSizeChanged(double width, double height)
    {
        if (width <= 0 || height <= 0) return;
        var drawingStart = _segmentStart is null ? null : Transform.ToModel(_segmentStart.Value);
        var previewEnd = _segmentStart is not null && IsPreviewVisible
            ? Transform.ToModel(new Point(PreviewX2, PreviewY2))
            : null;
        if (!_hasCanvasSize)
        {
            Transform.PanOrigin = new Point(width / 2, height / 2);
            _hasCanvasSize = true;
        }
        else
        {
            Transform.PanOrigin = new Point(
                Transform.PanOrigin.X + (width - _canvasWidth) / 2,
                Transform.PanOrigin.Y + (height - _canvasHeight) / 2);
        }
        _canvasWidth = width; _canvasHeight = height;
        RepositionDrawingPreview(drawingStart, previewEnd);
        IsSnapMarkerVisible = false;
        RefreshView();
    }

    public void HandleCanvasZoom(Point cursor, int wheelDelta)
    {
        if (wheelDelta == 0) return;
        var drawingStart = _segmentStart is null ? null : Transform.ToModel(_segmentStart.Value);
        var previewEnd = _segmentStart is not null && IsPreviewVisible
            ? Transform.ToModel(new Point(PreviewX2, PreviewY2))
            : null;
        var modelUnderCursor = Transform.ToModel(cursor);
        var factor = Math.Pow(1.1, wheelDelta / 120.0);
        Transform.PixelsPerMillimeter = Math.Clamp(Transform.PixelsPerMillimeter * factor, 0.005, 2.0);
        var screenAfter = Transform.ToScreen(modelUnderCursor);
        Transform.PanOrigin = new Point(
            Transform.PanOrigin.X + (cursor.X - screenAfter.X),
            Transform.PanOrigin.Y + (cursor.Y - screenAfter.Y));
        RepositionDrawingPreview(drawingStart, previewEnd);
        CursorX = cursor.X;
        CursorY = cursor.Y;
        IsCursorVisible = true;
        IsSnapMarkerVisible = false;
        RefreshView();
    }

    public void HandleCanvasPan(double dxPixels, double dyPixels)
    {
        if (dxPixels == 0 && dyPixels == 0) return;
        var drawingStart = _segmentStart is null ? null : Transform.ToModel(_segmentStart.Value);
        var previewEnd = _segmentStart is not null && IsPreviewVisible
            ? Transform.ToModel(new Point(PreviewX2, PreviewY2))
            : null;
        Transform.PanOrigin = new Point(Transform.PanOrigin.X + dxPixels, Transform.PanOrigin.Y + dyPixels);
        RepositionDrawingPreview(drawingStart, previewEnd);
        CursorX += dxPixels;
        CursorY += dyPixels;
        IsCursorVisible = true;
        IsSnapMarkerVisible = false;
        RefreshView();
    }

    private void RepositionDrawingPreview(
        DHBIMWATER.Core.Geometry.Point2D? drawingStart,
        DHBIMWATER.Core.Geometry.Point2D? previewEnd)
    {
        if (drawingStart is null) return;
        var startScreen = Transform.ToScreen(drawingStart);
        _segmentStart = startScreen;
        PreviewX1 = startScreen.X;
        PreviewY1 = startScreen.Y;
        if (previewEnd is null) return;
        var endScreen = Transform.ToScreen(previewEnd);
        PreviewX2 = endScreen.X;
        PreviewY2 = endScreen.Y;
        PreviewLengthX = (PreviewX1 + PreviewX2) / 2;
        PreviewLengthY = (PreviewY1 + PreviewY2) / 2;
        UpdateTempDimensions(previewEnd);
    }

    private void RefreshView()
    {
        OnPropertyChanged(nameof(ReferenceScreenX));
        OnPropertyChanged(nameof(ReferenceScreenY));
        RefreshGraph();
        RefreshOutline();
    }

    private PipeSnapResult? FindSnapPoint(
        DHBIMWATER.Core.Geometry.Point2D point,
        DHBIMWATER.Core.Geometry.Point2D? drawingStart = null) =>
        FindSnapPoint(point, drawingStart, out _);

    private PipeSnapResult? FindSnapPoint(
        DHBIMWATER.Core.Geometry.Point2D point,
        DHBIMWATER.Core.Geometry.Point2D? drawingStart,
        out bool filtered)
    {
        const double tolerance = 1e-6;
        filtered = false;
        var mode = PipeSnapMode.None;
        if (SnapEndpoint) mode |= PipeSnapMode.Endpoint;
        if (SnapMidpoint) mode |= PipeSnapMode.Midpoint;
        if (SnapQuadrant) mode |= PipeSnapMode.Quadrant;
        if (SnapIntersection) mode |= PipeSnapMode.Intersection;
        if (SnapNearest) mode |= PipeSnapMode.Nearest;
        var networkSnap = _network.FindSnapPoint(point, mode);
        var referencePoint = new DHBIMWATER.Core.Geometry.Point2D(0, 0);
        PipeSnapResult? referenceSnap = SnapReferencePoint && point.DistanceTo(referencePoint) <= PipeTopologyBuilder.SnapTolerance
            ? new PipeSnapResult(referencePoint, PipeSnapKind.Intersection, point.DistanceTo(referencePoint))
            : null;

        // 정척 상한 안의 후보만 남긴 뒤 종류 우선순위, 같은 종류에서는 거리 순으로 고른다.
        PipeSnapResult? best = null;
        foreach (var candidate in OutlineSnapCandidates(point).Concat(ArrowSnapCandidates(point)).Concat(ReferenceLineSnapCandidates(point)).Append(networkSnap).Append(referenceSnap))
        {
            if (candidate is null) continue;
            if (drawingStart is not null
                && drawingStart.DistanceTo(candidate.Value.Point) > StraightLengthMm + tolerance)
            {
                filtered = true;
                continue;
            }
            if (best is null
                || PipeTopologyBuilder.Rank(candidate.Value.Kind) < PipeTopologyBuilder.Rank(best.Value.Kind)
                || PipeTopologyBuilder.Rank(candidate.Value.Kind) == PipeTopologyBuilder.Rank(best.Value.Kind)
                    && candidate.Value.Distance < best.Value.Distance)
                best = candidate;
        }
        return best;
    }

    /// <summary>연두색 X/Y 기준선에 내린 수선의 발을 근처점 후보로 낸다. 두 기준선은 캔버스 모델 좌표의 X=0, Y=0이다.</summary>
    private IEnumerable<PipeSnapResult?> ReferenceLineSnapCandidates(DHBIMWATER.Core.Geometry.Point2D point)
    {
        if (!SnapNearest) yield break;
        if (Math.Abs(point.X) <= PipeTopologyBuilder.SnapTolerance)
            yield return new PipeSnapResult(new DHBIMWATER.Core.Geometry.Point2D(0, point.Y), PipeSnapKind.Nearest, Math.Abs(point.X));
        if (Math.Abs(point.Y) <= PipeTopologyBuilder.SnapTolerance)
            yield return new PipeSnapResult(new DHBIMWATER.Core.Geometry.Point2D(point.X, 0), PipeSnapKind.Nearest, Math.Abs(point.Y));
    }

    private IEnumerable<PipeSnapResult?> OutlineSnapCandidates(DHBIMWATER.Core.Geometry.Point2D point)
    {
        if (!SnapOutline || _canvasOutline.IsEmpty) yield break;
        var candidates = Enumerable.Empty<(DHBIMWATER.Core.Geometry.Point2D Point, PipeSnapKind Kind)>();
        if (SnapEndpoint) candidates = candidates.Concat(_canvasOutline.Endpoints().Select(x => (x, PipeSnapKind.Endpoint)));
        if (SnapMidpoint) candidates = candidates.Concat(_canvasOutline.Midpoints().Select(x => (x, PipeSnapKind.Midpoint)));
        if (SnapNearest) candidates = candidates.Concat(_canvasOutline.NearestPoints(point).Select(x => (x, PipeSnapKind.Nearest)));
        foreach (var candidate in candidates)
        {
            var distance = point.DistanceTo(candidate.Point);
            if (distance <= PipeTopologyBuilder.SnapTolerance)
                yield return new PipeSnapResult(candidate.Point, candidate.Kind, distance);
        }
    }

    /// <summary>IN/OUT 화살표의 벽면 접점을 끝점 스냅 후보로 낸다.</summary>
    private IEnumerable<PipeSnapResult?> ArrowSnapCandidates(DHBIMWATER.Core.Geometry.Point2D point)
    {
        if (!SnapEndpoint) yield break;
        foreach (var anchor in _arrowAnchors)
        {
            var distance = point.DistanceTo(anchor);
            if (distance <= PipeTopologyBuilder.SnapTolerance)
                yield return new PipeSnapResult(anchor, PipeSnapKind.Endpoint, distance);
        }
    }

    private void PickOutline()
    {
        CancelDrawing();
        Status = "Revit 뷰에서 외곽 벽체를 선택하세요.";
        PickOutlineAction?.Invoke();
    }

    /// <summary>피킹 결과를 반영한다. <paramref name="outline"/>가 null이면 사용자가 취소한 것으로 본다.</summary>
    public void ApplyOutline(ValveRoomOutline? outline)
    {
        if (outline is null || outline.IsEmpty)
        {
            Status = "외곽 벽체 선택을 취소했습니다.";
            return;
        }
        _outline = outline;

        // 실내 중심이 원점(0,0)에 오도록 기준점을 자동 설정한다. 모델 생성 시 이 값이 다시 더해져 원위치된다.
        var centroid = outline.Centroid;
        _referenceX = Math.Round(centroid.X, 1); OnPropertyChanged(nameof(ReferenceX));
        _referenceY = Math.Round(centroid.Y, 1); OnPropertyChanged(nameof(ReferenceY));

        // 화살표 상하 위치 기본값: 내부 세로 길이의 절반 → 상단/하단 기준 어느 쪽이든 세로 중앙에 온다.
        // 사용자가 직접 입력한 값은 덮어쓰지 않는다(확정 사항). 프로퍼티 setter는 편집 플래그를 세우므로 백킹 필드에 직접 대입한다.
        var halfHeight = Math.Round(outline.HeightMm / 2, 1);
        if (!_inOffsetUserEdited) { _inOffsetMm = halfHeight; OnPropertyChanged(nameof(InOffsetMm)); }
        if (!_outOffsetUserEdited) { _outOffsetMm = halfHeight; OnPropertyChanged(nameof(OutOffsetMm)); }

        Transform.PanOrigin = new Point(_canvasWidth / 2, _canvasHeight / 2);
        ZoomToFit(outline);
        RefreshView();
        Status = $"외곽 벽체 {outline.Walls.Count}장을 반영했습니다. (내부 {outline.WidthMm / 1000:N2} × {outline.HeightMm / 1000:N2} m)";
    }

    private void ClearOutline()
    {
        _outline = ValveRoomOutline.Empty;
        _inOffsetUserEdited = _outOffsetUserEdited = false;   // 다음 피킹 때 다시 자동값이 들어오도록 초기화
        RefreshOutline();
        Status = "외곽 레이아웃을 지웠습니다.";
    }

    /// <summary>외곽 전체가 캔버스에 여유 있게 들어오도록 축척을 맞춘다.</summary>
    private void ZoomToFit(ValveRoomOutline outline)
    {
        if (_canvasWidth <= 0 || _canvasHeight <= 0) return;
        if (outline.WidthMm <= 0 || outline.HeightMm <= 0) return;
        var scale = Math.Min(_canvasWidth * 0.8 / outline.WidthMm, _canvasHeight * 0.8 / outline.HeightMm);
        Transform.PixelsPerMillimeter = Math.Clamp(scale, 0.005, 2.0);
    }

    /// <summary>원본 외곽을 기준점 기준 캔버스 좌표로 옮기고 벽·화살표 표시 항목을 다시 만든다.</summary>
    private void RefreshOutline()
    {
        _canvasOutline = _outline.IsEmpty
            ? ValveRoomOutline.Empty
            : _outline.Translate(new DHBIMWATER.Core.Geometry.Point2D(-ReferenceX, -ReferenceY));

        OutlineWalls.Clear();
        foreach (var wall in _canvasOutline.Walls)
        {
            var innerA = Transform.ToScreen(wall.InnerStart); var innerB = Transform.ToScreen(wall.InnerEnd);
            var outerA = Transform.ToScreen(wall.OuterStart); var outerB = Transform.ToScreen(wall.OuterEnd);
            OutlineWalls.Add(new OutlineWallItem(innerA.X, innerA.Y, innerB.X, innerB.Y, outerA.X, outerA.Y, outerB.X, outerB.Y, wall.ThicknessMm));
        }

        RefreshArrows();
        OnPropertyChanged(nameof(HasOutline));
        NotifyValidationChanged();
    }

    private void RefreshArrows()
    {
        OutlineArrows.Clear();
        _arrowAnchors.Clear();
        if (_canvasOutline.IsEmpty) return;

        // 화살표는 좌/우 내측면에 고정하고, 사용자는 상하 위치만 지정한다(확정 사항).
        OutlineArrows.Add(BuildArrow("IN", _canvasOutline.LeftFaceX, InOffsetMm));
        OutlineArrows.Add(BuildArrow("OUT", _canvasOutline.RightFaceX, OutOffsetMm));
    }

    /// <summary>IN은 좌측 벽 바깥에서 벽면까지, OUT은 우측 벽면에서 바깥으로. 둘 다 화살표는 +X를 향한다.</summary>
    private OutlineArrowItem BuildArrow(string label, double faceX, double offsetMm)
    {
        const double ShaftPx = 46, HeadPx = 10;
        var y = ArrowFromTop ? _canvasOutline.MaxY - offsetMm : _canvasOutline.MinY + offsetMm;
        var anchorModel = new DHBIMWATER.Core.Geometry.Point2D(faceX, y);
        _arrowAnchors.Add(anchorModel);   // 벽면 접점을 끝점 스냅 후보로 등록
        var anchor = Transform.ToScreen(anchorModel);

        var isIn = label == "IN";
        var tail = isIn ? anchor.X - ShaftPx : anchor.X;
        var tip = isIn ? anchor.X : anchor.X + ShaftPx;
        // Polygon.Points는 문자열 TypeConverter로 파싱되므로 로캘 무관하게 InvariantCulture로 만든다.
        var head = Points((tip, anchor.Y), (tip - HeadPx, anchor.Y - 5), (tip - HeadPx, anchor.Y + 5));
        var labelX = isIn ? tail - 26 : tip + 4;
        return new OutlineArrowItem(label, tail, anchor.Y, tip, anchor.Y, head, labelX, anchor.Y - 9, offsetMm);
    }

    private static string Points(params (double X, double Y)[] points)
        => string.Join(" ", points.Select(p => FormattableString.Invariant($"{p.X},{p.Y}")));

    /// <summary>커서(또는 스냅된 점)에서 X·Y 각 방향으로 가장 가까운 외곽선까지의 임시치수를 만든다.</summary>
    private void UpdateTempDimensions(DHBIMWATER.Core.Geometry.Point2D point)
    {
        TempDimensions.Clear();
        if (_canvasOutline.IsEmpty) return;
        Add(_canvasOutline.MeasureHorizontal(point));
        Add(_canvasOutline.MeasureVertical(point));

        void Add(OutlineMeasure? measure)
        {
            if (measure is null) return;
            var a = Transform.ToScreen(measure.Value.From);
            var b = Transform.ToScreen(measure.Value.Hit);
            TempDimensions.Add(new TempDimensionItem(a.X, a.Y, b.X, b.Y, measure.Value.DistanceMm, (a.X + b.X) / 2, (a.Y + b.Y) / 2));
        }
    }
    public void SelectEdge(Guid edgeId)
    {
        if (!IsSelectionMode) return;
        SelectedEdge = Edges.FirstOrDefault(x => x.Id == edgeId);
        SelectedFitting = null;
        RefreshGraph();
    }

    public void SelectFitting(Guid fittingId)
    {
        if (!IsSelectionMode) return;
        SelectedFitting = InlineFittings.FirstOrDefault(x => x.Id == fittingId);
        SelectedEdge = null;
        RefreshGraph();
    }

    public void HandleEscape()
    {
        if (IsDrawing) { CancelDrawing(); return; }
        if (IsPlacingFitting) { SelectedFamilyTypeName = null; Status = "부속 배치를 취소했습니다."; return; }
        if (SelectedFitting is not null) { SelectedFitting = null; RefreshGraph(); Status = "선택을 취소했습니다."; return; }
        if (SelectedEdge is null) return;
        SelectedEdge = null;
        RefreshGraph();
        Status = "선택을 취소했습니다.";
    }

    public void DeleteSelectedEdge()
    {
        if (SelectedEdge is null) return;
        var before = _network.DeepClone();
        _network.RemoveSegment(SelectedEdge.Id);
        RecordUndoIfChanged(before);
        SelectedEdge = null;
        RefreshGraph();
        Status = "선택한 배관을 삭제했습니다.";
    }

    private void DeleteSelectedFitting()
    {
        if (SelectedFitting is null) return;
        RemoveFitting(SelectedFitting);
        Status = "선택한 부속을 삭제했습니다.";
    }

    /// <summary>인라인 밸브는 배관 밸브류, 관·절점부속은 배관 부속류 패밀리를 보여준다.</summary>
    private void RefreshFamilyTypeNames()
    {
        var accessoryNames = _typeRepo.GetPipeAccessoryTypeNames().ToList();   // 인라인 밸브류
        var fittingNames = _typeRepo.GetPipeFittingTypeNames().ToList();       // 관·곡관·T형
        FamilyTypeNames.Clear();
        SegmentFamilyTypeNames.Clear();
        foreach (var name in accessoryNames) FamilyTypeNames.Add(name);
        foreach (var name in fittingNames) SegmentFamilyTypeNames.Add(name);
        SelectedFamilyTypeName = null;

        // 이름으로 기본값을 추정한다. 못 찾으면 비워 두고 검증에서 선택을 요구한다.
        _segmentFamilyTypeName ??= GuessFamily(fittingNames, "직관", "단관");
        _bendFamilyTypeName ??= GuessFamily(fittingNames, "곡관");
        _teeFamilyTypeName ??= GuessFamily(fittingNames, "T형", "티", "TEE");
        OnPropertyChanged(nameof(SegmentFamilyTypeName));
        OnPropertyChanged(nameof(BendFamilyTypeName));
        OnPropertyChanged(nameof(TeeFamilyTypeName));
        RefreshSegmentParameterNames();
    }

    private static string? GuessFamily(IReadOnlyList<string> names, params string[] keywords)
        => names.FirstOrDefault(name => keywords.Any(k => name.Contains(k, StringComparison.OrdinalIgnoreCase)));

    /// <summary>선택된 관 패밀리의 인스턴스 파라미터를 다시 읽고, 사용자가 직접 고르지 않은 길이·직경 이름을 자동 추정한다.</summary>
    private void RefreshSegmentParameterNames()
    {
        LengthParameterNames.Clear();
        // TODO: 실제로는 카테고리 공통 조회이므로 인터페이스 메서드명을 별도 작업에서 일반화한다.
        var names = string.IsNullOrWhiteSpace(SegmentFamilyTypeName)
            ? []
            : _typeRepo.GetPipeAccessoryInstanceParameterNames(SegmentFamilyTypeName).ToList();
        foreach (var name in names) LengthParameterNames.Add(name);

        // 사용자가 고른 값이 새 패밀리에도 있으면 그대로 둔다.
        if (!(_lengthParameterPinned && _lengthParameterName is not null && names.Contains(_lengthParameterName)))
        {
            _lengthParameterName = names.FirstOrDefault(x => x.Contains("길이", StringComparison.Ordinal))
                ?? names.FirstOrDefault(x => x.Contains("Length", StringComparison.OrdinalIgnoreCase))
                ?? names.FirstOrDefault(x => string.Equals(x, "L", StringComparison.OrdinalIgnoreCase));
            _lengthParameterPinned = false;
            OnPropertyChanged(nameof(LengthParameterName));
        }

        if (!(_diameterParameterPinned && _diameterParameterName is not null && names.Contains(_diameterParameterName)))
        {
            _diameterParameterName = names.FirstOrDefault(x => x.Contains("직경", StringComparison.Ordinal))
                ?? names.FirstOrDefault(x => x.Contains("지름", StringComparison.Ordinal))
                ?? names.FirstOrDefault(x => x.Contains("호칭", StringComparison.Ordinal))
                ?? names.FirstOrDefault(x => x.Contains("Diameter", StringComparison.OrdinalIgnoreCase))
                ?? names.FirstOrDefault(x => string.Equals(x, "DN", StringComparison.OrdinalIgnoreCase));
            _diameterParameterPinned = false;
            OnPropertyChanged(nameof(DiameterParameterName));
        }
    }

    private void RemoveFitting(InlineFittingItem? fitting)
    {
        if (fitting is null) return;
        // TODO: 분할 후에도 엣지 ID를 보존하는 Phase 2 편집 API를 추가할 수 있다.
        var edge = _network.Edges.FirstOrDefault(x => x.Id == fitting.EdgeId);
        if (edge is null) return;
        var before = _network.DeepClone();
        var wasSelected = SelectedEdge?.Id == edge.Id;
        _network.RemoveInlineFitting(edge.Id, fitting.Id);
        RecordUndoIfChanged(before);
        RefreshGraph();
        if (wasSelected)
            SelectedEdge = Edges.FirstOrDefault(x => x.StartNodeId == edge.StartNodeId && x.EndNodeId == edge.EndNodeId);
        if (SelectedFitting?.Id == fitting.Id) SelectedFitting = null;
    }

    public void UpdateInputValidation(bool errorAdded)
    {
        _inputValidationErrorCount = Math.Max(0, _inputValidationErrorCount + (errorAdded ? 1 : -1));
        NotifyValidationChanged();
    }

    public void ApplyModelCreationResult(bool succeeded, string message)
    {
        IsCreating = false;
        Status = succeeded ? $"배관 생성을 완료했습니다. {message}" : $"배관 생성 실패: {message}";
    }

    private void Undo()
    {
        if (_undoHistory.Count == 0) return;
        _network = _undoHistory.Pop();
        _elevation = _network.Elevation;
        OnPropertyChanged(nameof(Elevation));
        SetSegmentStart(null);
        SelectedEdge = null;
        IsPreviewVisible = false;
        IsSnapMarkerVisible = false;
        HideFittingPreview();
        TempDimensions.Clear();
        RefreshGraph();
        Status = "마지막 편집을 되돌렸습니다.";
    }

    private void RecordUndoIfChanged(PipeNetwork before)
    {
        if (SameNetwork(before, _network)) return;
        _undoHistory.Push(before);
        NotifyValidationChanged();
    }

    private static bool SameNetwork(PipeNetwork a, PipeNetwork b)
    {
        if (a.Nodes.Count != b.Nodes.Count || a.Edges.Count != b.Edges.Count) return false;
        var aNodes = a.Nodes.OrderBy(x => x.Id).ToList();
        var bNodes = b.Nodes.OrderBy(x => x.Id).ToList();
        if (!aNodes.Zip(bNodes).All(x => x.First.Id == x.Second.Id && x.First.Position == x.Second.Position && x.First.NodeKind == x.Second.NodeKind)) return false;
        var aEdges = a.Edges.OrderBy(x => x.Id).ToList();
        var bEdges = b.Edges.OrderBy(x => x.Id).ToList();
        return aEdges.Zip(bEdges).All(x => x.First.Id == x.Second.Id &&
            x.First.StartNodeId == x.Second.StartNodeId && x.First.EndNodeId == x.Second.EndNodeId &&
            x.First.InlineFittings.SequenceEqual(x.Second.InlineFittings));
    }

    private void SetSegmentStart(Point? value)
    {
        _segmentStart = value;
        OnPropertyChanged(nameof(IsDrawing));
        OnPropertyChanged(nameof(InteractionModeText));
        CommandManager.InvalidateRequerySuggested();
    }

    private bool CanCreateModel() => CreateModelAction is not null && !IsCreating && GetValidationErrors().Count == 0;

    private List<string> GetValidationErrors()
    {
        var errors = new List<string>();
        if (_inputValidationErrorCount > 0) errors.Add("빨간색으로 표시된 숫자 입력을 확인하세요.");
        if (_network.Edges.Count == 0) errors.Add("배관을 한 개 이상 그려야 합니다.");
        // MEP Pipe를 만들지 않으므로 시스템 타입·PipeType은 필수가 아니다(콤보는 향후 병행을 위해 남겨 둔다).
        if (string.IsNullOrWhiteSpace(SelectedLevelName)) errors.Add("레벨을 선택하세요.");
        if (string.IsNullOrWhiteSpace(SegmentFamilyTypeName)) errors.Add("관 패밀리를 선택하세요.");
        if (string.IsNullOrWhiteSpace(LengthParameterName)) errors.Add("관 길이 파라미터를 선택하세요.");
        if (!double.IsFinite(StraightLengthMm) || StraightLengthMm <= 0) errors.Add("정척 길이는 0보다 커야 합니다.");
        if (!double.IsFinite(DiameterMm) || DiameterMm <= 0) errors.Add("관경은 0보다 커야 합니다.");
        if (!double.IsFinite(Elevation) || !double.IsFinite(ReferenceX) || !double.IsFinite(ReferenceY)) errors.Add("좌표와 표고는 유효한 숫자여야 합니다.");
        if (InOffsetMm < 0 || OutOffsetMm < 0) errors.Add("IN/OUT 이격은 0 이상이어야 합니다.");
        if (!_canvasOutline.IsEmpty && (InOffsetMm > _canvasOutline.HeightMm || OutOffsetMm > _canvasOutline.HeightMm))
            errors.Add("IN/OUT 이격이 외곽 내부 높이를 벗어났습니다.");

        if (_network.Edges.Count > 0)
        {
            var definition = _network.ToDefinition(DiameterMm, PipeOutputMode.PipeAccessorySegment, new(ReferenceX, ReferenceY));
            var invalidTeeCount = definition.Nodes.Count(x => x.NodeKind == NodeKind.Tee && PipeTeeResolver.Resolve(x, definition.Edges) is null);
            if (invalidTeeCount > 0) errors.Add($"직교 T가 아닌 3방향 분기 {invalidTeeCount}개를 수정하세요.");

            var elbowCount = definition.Nodes.Count(x => x.NodeKind == NodeKind.Elbow);
            if (elbowCount > 0 && string.IsNullOrWhiteSpace(BendFamilyTypeName)) errors.Add("곡관 패밀리를 선택하세요.");

            if (definition.Nodes.Any(x => x.NodeKind == NodeKind.Tee) && string.IsNullOrWhiteSpace(TeeFamilyTypeName)) errors.Add("T형 패밀리를 선택하세요.");
            var crossCount = definition.Nodes.Count(x => x.NodeKind == NodeKind.Cross);
            if (crossCount > 0) errors.Add($"4방향 교차 절점 {crossCount}개는 아직 지원하지 않습니다. 십자 부속은 후속 과제입니다.");
        }
        return errors;
    }

    private void NotifyValidationChanged()
    {
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(NetworkSummary));
        OnPropertyChanged(nameof(ValidationSummary));
        OnPropertyChanged(nameof(InteractionModeText));
        CommandManager.InvalidateRequerySuggested();
    }

    private void CreateModel()
    {
        var errors = GetValidationErrors();
        if (errors.Count > 0) { Status = errors[0]; return; }
        IsCreating = true;
        try
        {
            CreateModelAction?.Invoke(_network.ToDefinition(DiameterMm, PipeOutputMode.PipeAccessorySegment, new(ReferenceX, ReferenceY),
                "", "", SelectedLevelName!,
                new PipeSegmentFamilySelection(
                    SegmentFamilyTypeName!, LengthParameterName!,
                    BendFamilyTypeName ?? "", TeeFamilyTypeName ?? "",
                    StraightLengthMm, DiameterParameterName ?? "")));
            Status = "Revit에서 직관·단관을 배치하고 있습니다…";
        }
        catch (Exception ex)
        {
            IsCreating = false;
            Status = $"관 생성 요청 실패: {ex.Message}";
        }
    }

    private void Clear()
    {
        if (_network.Edges.Count == 0) return;
        var before = _network.DeepClone();
        _network.Clear();
        RecordUndoIfChanged(before);
        SetSegmentStart(null);
        SelectedEdge = null;
        IsPreviewVisible = false;
        IsSnapMarkerVisible = false;
        HideFittingPreview();
        TempDimensions.Clear();
        RefreshGraph();
        Status = "그래프를 초기화했습니다.";
    }
    private void CancelDrawing() { SetSegmentStart(null); IsPreviewVisible = false; IsSnapMarkerVisible = false; TempDimensions.Clear(); Status = "그리기를 취소했습니다."; }

    private static string GetSnapMarkerSymbol(PipeSnapResult snap) => snap.Kind switch
    {
        PipeSnapKind.Intersection => "×",
        PipeSnapKind.Endpoint => "□",
        PipeSnapKind.Midpoint => "△",
        PipeSnapKind.Quadrant => "◇",
        _ => "·",
    };

    private static string GetSnapMarkerLabel(PipeSnapResult snap) => snap.Kind switch
    {
        PipeSnapKind.Intersection => "교차점",
        PipeSnapKind.Endpoint => "끝점",
        PipeSnapKind.Midpoint => "중간점",
        PipeSnapKind.Quadrant => "사분점",
        _ => "근처점",
    };

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
        NotifyValidationChanged();
    }

    /// <summary>전체 네트워크의 부속을 보여준다(선택된 엣지만이 아니라). 클릭-배치 흐름에는 엣지 선택 단계가 없으므로
    /// 배치 직후에도 캔버스·사이드바에 남아 있어야 한다.</summary>
    private void RefreshFittings()
    {
        InlineFittings.Clear();
        foreach (var edge in _network.Edges)
        {
            var display = Edges.FirstOrDefault(x => x.Id == edge.Id);
            if (display is null) continue;
            foreach (var fitting in edge.InlineFittings.OrderBy(x => x.Order))
                InlineFittings.Add(new InlineFittingItem(fitting.Id, edge.Id, fitting.TypeKey, fitting.FamilyTypeName, fitting.T, fitting.Order,
                    display.X1 + (display.X2 - display.X1) * fitting.T, display.Y1 + (display.Y2 - display.Y1) * fitting.T,
                    fitting.Id == SelectedFitting?.Id));
        }
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
    public string Color => NodeKind switch { NodeKind.EndPoint => "#7F8C8D", NodeKind.Inline => "#3498DB", NodeKind.Elbow => "#F39C12", NodeKind.Tee => "#9B59B6", _ => "#E74C3C" };
}
public sealed record InlineFittingItem(Guid Id, Guid EdgeId, string TypeKey, string FamilyTypeName, double T, int Order, double X, double Y, bool IsSelected)
{
    public int DisplayOrder => Order + 1;
    public string Fill => IsSelected ? "#E67E22" : "#F39C12";
}

/// <summary>외곽 벽체 한 장의 화면 표시. 내측면(기준선)은 실선, 외측면은 두께 표현용 보조선이다.</summary>
public sealed record OutlineWallItem(
    double InnerX1, double InnerY1, double InnerX2, double InnerY2,
    double OuterX1, double OuterY1, double OuterX2, double OuterY2,
    double ThicknessMm)
{
    public string ThicknessLabel => $"t={ThicknessMm:N0}";
}

/// <summary>IN/OUT 유입·유출 화살표의 화면 표시.</summary>
public sealed record OutlineArrowItem(
    string Label, double X1, double Y1, double X2, double Y2,
    string HeadPoints, double LabelX, double LabelY, double OffsetMm)
{
    public string Color => Label == "IN" ? "#1E8449" : "#B03A2E";
}

/// <summary>스케치 중 외곽선까지의 임시치수 한 개.</summary>
public sealed record TempDimensionItem(double X1, double Y1, double X2, double Y2, double DistanceMm, double LabelX, double LabelY)
{
    public string Label => $"{DistanceMm:N0}";
}
