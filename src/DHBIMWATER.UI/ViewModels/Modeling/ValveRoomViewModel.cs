using DHBIMWATER.Application.DTOs.Revit.ValveRoom;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace DHBIMWATER.UI.ViewModels.Modeling;

/// <summary>
/// 독립 밸브실 모델링 입력 화면의 상태를 관리한다.
/// Revit 모델 생성은 후속 UseCase 연결 시점에 구현한다.
/// </summary>
public class ValveRoomViewModel : ViewModelBase
{
    private readonly IDialogService _dialogService;
    private readonly IElementTypeQueryRepo _typeQueryRepo;

    private string _selectedValveRoomType = "이토밸브실";
    private string _selectedColumnTypeName = "(사용 안 함)";
    private string _selectedBeamTypeName = "(사용 안 함)";
    private double _plainConcreteThickness = 100;
    private double _foundationThickness = 500;
    private double _foundationToe = 300;
    private double _outerWallThickness = 400;
    private double _intermediateWallThickness = 300;
    private double _upperSlabThickness = 400;
    private double _intermediateSlabThickness = 300;
    private double _innerWidth = 2500;
    private double _innerLength = 4000;
    private double _innerHeight = 2500;
    private bool _hasIntermediateWall = true;
    private int _intermediateWallCount = 1;
    private bool _hasIntermediateSlab;
    private double _floor1InnerHeight = 2500;
    private double _floor2InnerHeight = 2500;
    private int _beamCountX = 1;
    private double _beamOffsetX = 1000;
    private double _beamSpacingX = 1000;
    private int _beamCountY = 1;
    private double _beamOffsetY = 1000;
    private double _beamSpacingY = 1000;

    private double _sharedCoordinateX;
    private double _sharedCoordinateY;
    private double _sharedElevation;
    private double _trueNorthToProjectNorthClockwiseDegrees;
    public ValveRoomViewModel(IDialogService dialogService, IElementTypeQueryRepo typeQueryRepo)
    {
        _dialogService = dialogService;
        _typeQueryRepo = typeQueryRepo;

        ValveRoomTypes = new ObservableCollection<string>(new[] { "이토밸브실", "제수밸브실", "공기밸브실" });
        ColumnTypeNames = new ObservableCollection<string>(new[] { "(사용 안 함)" });
        BeamTypeNames = new ObservableCollection<string>(new[] { "(사용 안 함)" });
        LoadTypeNames();

        ResetCommand = new RelayCommand(_ => ApplyPreset());
        PreviewCommand = new RelayCommand(_ => PreviewRequested?.Invoke(new ValveRoomPreviewViewModel(this)));
        CreateCommand = new RelayCommand(_ => RequestCreate());
        CancelCommand = new RelayCommand(_ => CloseAction?.Invoke());
        ApplyPreset();
    }

    public ObservableCollection<string> ValveRoomTypes { get; }
    public ObservableCollection<string> ColumnTypeNames { get; }
    public ObservableCollection<string> BeamTypeNames { get; }
    public ICommand ResetCommand { get; }
    public ICommand PreviewCommand { get; }
    public ICommand CreateCommand { get; }
    public ICommand CancelCommand { get; }
    public Action? CloseAction { get; set; }
    public Action<ValveRoomPreviewViewModel>? PreviewRequested { get; set; }
    public ValveRoomRequestDto? RequestedCreate { get; private set; }

    public string SelectedValveRoomType
    {
        get => _selectedValveRoomType;
        set
        {
            if (!SetProperty(ref _selectedValveRoomType, value)) return;
            ApplyPreset();
            OnPropertyChanged(nameof(MudVisibility));
            OnPropertyChanged(nameof(SluiceVisibility));
            OnPropertyChanged(nameof(FrameVisibility));
            OnPropertyChanged(nameof(IntermediateWallVisibility));
            OnPropertyChanged(nameof(IntermediateSlabVisibility));
            OnPropertyChanged(nameof(SingleHeightVisibility));
            OnPropertyChanged(nameof(Summary));
        }
    }

    public string SelectedColumnTypeName { get => _selectedColumnTypeName; set => SetProperty(ref _selectedColumnTypeName, value); }
    public string SelectedBeamTypeName { get => _selectedBeamTypeName; set => SetProperty(ref _selectedBeamTypeName, value); }
    public double PlainConcreteThickness { get => _plainConcreteThickness; set => SetAndRefresh(ref _plainConcreteThickness, value); }
    public double FoundationThickness { get => _foundationThickness; set => SetAndRefresh(ref _foundationThickness, value); }
    public double FoundationToe { get => _foundationToe; set => SetAndRefresh(ref _foundationToe, value); }
    public double OuterWallThickness { get => _outerWallThickness; set => SetAndRefresh(ref _outerWallThickness, value); }
    public double IntermediateWallThickness { get => _intermediateWallThickness; set => SetProperty(ref _intermediateWallThickness, value); }
    public double UpperSlabThickness { get => _upperSlabThickness; set => SetProperty(ref _upperSlabThickness, value); }
    public double IntermediateSlabThickness { get => _intermediateSlabThickness; set => SetProperty(ref _intermediateSlabThickness, value); }
    public double InnerWidth { get => _innerWidth; set => SetAndRefresh(ref _innerWidth, value); }
    public double InnerLength { get => _innerLength; set => SetAndRefresh(ref _innerLength, value); }
    public double InnerHeight { get => _innerHeight; set => SetAndRefresh(ref _innerHeight, value); }
    public int IntermediateWallCount { get => _intermediateWallCount; set => SetAndRefresh(ref _intermediateWallCount, Math.Max(0, value)); }
    public double Floor1InnerHeight { get => _floor1InnerHeight; set => SetAndRefresh(ref _floor1InnerHeight, value); }
    public double Floor2InnerHeight { get => _floor2InnerHeight; set => SetAndRefresh(ref _floor2InnerHeight, value); }
    public int BeamCountX { get => _beamCountX; set => SetAndRefresh(ref _beamCountX, Math.Max(0, value)); }
    public double BeamOffsetX { get => _beamOffsetX; set => SetProperty(ref _beamOffsetX, value); }
    public double BeamSpacingX { get => _beamSpacingX; set => SetProperty(ref _beamSpacingX, value); }
    public int BeamCountY { get => _beamCountY; set => SetAndRefresh(ref _beamCountY, Math.Max(0, value)); }
    public double BeamOffsetY { get => _beamOffsetY; set => SetProperty(ref _beamOffsetY, value); }
    public double BeamSpacingY { get => _beamSpacingY; set => SetProperty(ref _beamSpacingY, value); }

    public double SharedCoordinateX { get => _sharedCoordinateX; set => SetProperty(ref _sharedCoordinateX, value); }
    public double SharedCoordinateY { get => _sharedCoordinateY; set => SetProperty(ref _sharedCoordinateY, value); }
    public double SharedElevation { get => _sharedElevation; set => SetProperty(ref _sharedElevation, value); }
    public double TrueNorthToProjectNorthClockwiseDegrees { get => _trueNorthToProjectNorthClockwiseDegrees; set => SetProperty(ref _trueNorthToProjectNorthClockwiseDegrees, value); }
    public bool HasIntermediateWall
    {
        get => _hasIntermediateWall;
        set
        {
            if (!SetProperty(ref _hasIntermediateWall, value)) return;
            OnPropertyChanged(nameof(IntermediateWallVisibility));
            OnPropertyChanged(nameof(Summary));
        }
    }

    public bool HasIntermediateSlab
    {
        get => _hasIntermediateSlab;
        set
        {
            if (!SetProperty(ref _hasIntermediateSlab, value)) return;
            OnPropertyChanged(nameof(IntermediateSlabVisibility));
            OnPropertyChanged(nameof(SingleHeightVisibility));
            OnPropertyChanged(nameof(Summary));
        }
    }

    public Visibility MudVisibility => IsMud ? Visibility.Visible : Visibility.Collapsed;
    public Visibility SluiceVisibility => IsSluice ? Visibility.Visible : Visibility.Collapsed;
    public Visibility FrameVisibility => IsSluice ? Visibility.Visible : Visibility.Collapsed;
    public Visibility IntermediateWallVisibility => IsMud && HasIntermediateWall ? Visibility.Visible : Visibility.Collapsed;
    public Visibility IntermediateSlabVisibility => IsMud && HasIntermediateSlab ? Visibility.Visible : Visibility.Collapsed;
    public Visibility SingleHeightVisibility => IsMud && HasIntermediateSlab ? Visibility.Collapsed : Visibility.Visible;

    public string Summary
    {
        get
        {
            var outerWidth = InnerWidth + OuterWallThickness * 2;
            var outerLength = InnerLength + OuterWallThickness * 2;
            var foundationWidth = outerWidth + FoundationToe * 2;
            var foundationLength = outerLength + FoundationToe * 2;
            var height = IsMud && HasIntermediateSlab ? Floor1InnerHeight + Floor2InnerHeight : InnerHeight;
            var members = IsMud
                ? $"기초 · 외벽 4개 · {(HasIntermediateWall ? $"중간벽 {IntermediateWallCount}개 · " : string.Empty)}{(HasIntermediateSlab ? "중간슬래브 · " : string.Empty)}상부슬래브"
                : IsSluice
                    ? $"사각형 기초 · 외벽 4개 · 상부슬래브 · 보 X{BeamCountX}/Y{BeamCountY}개 · 보 교차부 기둥"
                    : "기초 · 외벽 4개 · 상부슬래브";

            return $"{SelectedValveRoomType} · 외벽 외곽 {outerWidth:N0}×{outerLength:N0} mm · " +
                   $"기초 {foundationWidth:N0}×{foundationLength:N0} mm (Toe {FoundationToe:N0} mm, 내부 H {height:N0} mm)\n{members}";
        }
    }

    private bool IsMud => SelectedValveRoomType == "이토밸브실";
    private bool IsSluice => SelectedValveRoomType == "제수밸브실";

    private void ApplyPreset()
    {
        if (IsMud)
        {
            _innerWidth = 2500; _innerLength = 4000; _innerHeight = 2500;
            _hasIntermediateWall = true; _intermediateWallCount = 1; _hasIntermediateSlab = false;
        }
        else if (IsSluice)
        {
            _innerWidth = 2000; _innerLength = 3000; _innerHeight = 2500;
        }
        else
        {
            _innerWidth = 1500; _innerLength = 1800; _innerHeight = 2000;
        }

        OnPropertyChanged(nameof(InnerWidth));
        OnPropertyChanged(nameof(InnerLength));
        OnPropertyChanged(nameof(InnerHeight));
        OnPropertyChanged(nameof(HasIntermediateWall));
        OnPropertyChanged(nameof(IntermediateWallCount));
        OnPropertyChanged(nameof(HasIntermediateSlab));
        OnPropertyChanged(nameof(IntermediateWallVisibility));
        OnPropertyChanged(nameof(IntermediateSlabVisibility));
        OnPropertyChanged(nameof(SingleHeightVisibility));
        OnPropertyChanged(nameof(Summary));
    }

    private void RequestCreate()
    {
        if (InnerWidth <= 0 || InnerLength <= 0 || FoundationThickness <= 0 || OuterWallThickness <= 0 ||
            (HasIntermediateSlab && (Floor1InnerHeight <= 0 || Floor2InnerHeight <= 0)))
        {
            _dialogService.Warn("입력 확인", "밸브실 치수와 두께는 0보다 커야 합니다.");
            return;
        }

        if (IsSluice && (SelectedBeamTypeName == "(사용 안 함)" || SelectedColumnTypeName == "(사용 안 함)"))
        {
            _dialogService.Warn("입력 확인", "제수밸브실은 보 유형과 기둥 유형을 선택해야 합니다.");
            return;
        }

        RequestedCreate = new ValveRoomRequestDto
        {
            RoomType = SelectedValveRoomType,
            PlainConcreteThickness = PlainConcreteThickness,
            FoundationThickness = FoundationThickness,
            SharedCoordinateX = SharedCoordinateX,
            SharedCoordinateY = SharedCoordinateY,
            SharedElevation = SharedElevation,
            TrueNorthToProjectNorthClockwiseDegrees = TrueNorthToProjectNorthClockwiseDegrees,
            FoundationToe = FoundationToe,
            OuterWallThickness = OuterWallThickness,
            IntermediateWallThickness = IntermediateWallThickness,
            UpperSlabThickness = UpperSlabThickness,
            IntermediateSlabThickness = IntermediateSlabThickness,
            InnerWidth = InnerWidth,
            InnerLength = InnerLength,
            InnerHeight = InnerHeight,
            HasIntermediateWall = HasIntermediateWall,
            IntermediateWallCount = IntermediateWallCount,
            HasIntermediateSlab = HasIntermediateSlab,
            Floor1InnerHeight = Floor1InnerHeight,
            Floor2InnerHeight = Floor2InnerHeight,
            BeamCountX = BeamCountX,
            BeamOffsetX = BeamOffsetX,
            BeamSpacingX = BeamSpacingX,
            BeamCountY = BeamCountY,
            BeamOffsetY = BeamOffsetY,
            BeamSpacingY = BeamSpacingY,
            BeamTypeName = SelectedBeamTypeName == "(사용 안 함)" ? string.Empty : SelectedBeamTypeName,
            ColumnTypeName = SelectedColumnTypeName == "(사용 안 함)" ? string.Empty : SelectedColumnTypeName
        };
        CloseAction?.Invoke();
    }
    private void LoadTypeNames()
    {
        foreach (var name in _typeQueryRepo.GetColumnTypeNames().Where(name => !string.IsNullOrWhiteSpace(name)).Distinct())
            ColumnTypeNames.Add(name);
        foreach (var name in _typeQueryRepo.GetBeamTypeNames().Where(name => !string.IsNullOrWhiteSpace(name)).Distinct())
            BeamTypeNames.Add(name);
    }

    private void SetAndRefresh<T>(ref T field, T value)
    {
        if (!SetProperty(ref field, value)) return;
        OnPropertyChanged(nameof(Summary));
    }
}