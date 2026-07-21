using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;

namespace DHBIMWATER.UI.ViewModels.Modeling;

public sealed class AlignmentPlacementFileItem : ViewModelBase
{
    public required string Path { get; init; }
    private string _pipeKind = string.Empty;
    public string FileName => System.IO.Path.GetFileName(Path);
    public string PipeKind { get => _pipeKind; set => SetProperty(ref _pipeKind, value); }
}

public sealed class AlignmentFamilyPlacementViewModel : ViewModelBase
{
    private readonly IFileDialogService _fileDialog; private readonly IElementTypeQueryRepo _typeRepo; private readonly ILevelQueryRepo _levelRepo; private readonly IDialogService _dialog;
    private AlignmentPlacementFileItem? _selectedFile; private bool _isBeamTarget = true, _alignTangent = true; private string? _selectedTargetTypeName, _pipeTypeName, _levelName; private double _intervalM = 6;
    public AlignmentFamilyPlacementViewModel(IFileDialogService fileDialog, IElementTypeQueryRepo typeRepo, ILevelQueryRepo levelRepo, IDialogService dialog)
    { _fileDialog = fileDialog; _typeRepo = typeRepo; _levelRepo = levelRepo; _dialog = dialog; AddCommand = new RelayCommand(_ => AddFile()); RemoveCommand = new RelayCommand(_ => RemoveFile()); CreateCommand = new RelayCommand(_ => RequestPlacement()); CancelCommand = new RelayCommand(_ => CloseAction?.Invoke()); }
    public ObservableCollection<AlignmentPlacementFileItem> Files { get; } = new();
    public AlignmentPlacementFileItem? SelectedFile { get => _selectedFile; set => SetProperty(ref _selectedFile, value); }
    public bool IsBeamTarget { get => _isBeamTarget; set { if (SetProperty(ref _isBeamTarget, value)) { OnPropertyChanged(nameof(TargetTypeNames)); OnPropertyChanged(nameof(IsPipingTarget)); SelectedTargetTypeName = TargetTypeNames.FirstOrDefault(); } } }
    public bool IsPipingTarget { get => !IsBeamTarget; set { if (value) IsBeamTarget = false; } }
    public IEnumerable<string> TargetTypeNames => IsBeamTarget ? _typeRepo.GetBeamTypeNames() : _typeRepo.GetPipingSystemTypeNames();
    public IEnumerable<string> PipeTypeNames => _typeRepo.GetPipeTypeNames(); public IEnumerable<string> LevelNames => _levelRepo.GetExistingLevelNames();
    public string? SelectedTargetTypeName { get => _selectedTargetTypeName; set => SetProperty(ref _selectedTargetTypeName, value); }
    public string? PipeTypeName { get => _pipeTypeName; set => SetProperty(ref _pipeTypeName, value); }
    public string? LevelName { get => _levelName; set => SetProperty(ref _levelName, value); }
    public double IntervalM { get => _intervalM; set => SetProperty(ref _intervalM, value); }
    public bool AlignTangent { get => _alignTangent; set => SetProperty(ref _alignTangent, value); }
    public ICommand AddCommand { get; } public ICommand RemoveCommand { get; } public ICommand CreateCommand { get; } public ICommand CancelCommand { get; }
    public Action? CloseAction { get; set; } public AlignmentFamilyPlacementRequest? RequestedPlacement { get; private set; }
    private void AddFile() { var path = _fileDialog.OpenFile("관로 선형 파일 선택", "관로 선형 파일 (*.shp;*.dxf)|*.shp;*.dxf|모든 파일 (*.*)|*.*"); if (!string.IsNullOrWhiteSpace(path) && !Files.Any(x => string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase))) { var item = new AlignmentPlacementFileItem { Path = path }; Files.Add(item); SelectedFile = item; } }
    private void RemoveFile() { if (SelectedFile is null) return; Files.Remove(SelectedFile); SelectedFile = Files.FirstOrDefault(); }
    private void RequestPlacement()
    {
        if (Files.Count == 0) { _dialog.Warn("입력 확인", "하나 이상의 SHP 또는 DXF 파일을 추가하세요."); return; }
        if (IntervalM <= 0) { _dialog.Warn("입력 확인", "배치 간격은 0보다 커야 합니다."); return; }
        RequestedPlacement = new AlignmentFamilyPlacementRequest { Files = Files.Select(x => new AlignmentPlacementFile(x.Path, x.PipeKind)).ToList(), IntervalMm = IntervalM * 1000, Target = IsBeamTarget ? AlignmentPlacementTarget.Beam : AlignmentPlacementTarget.PipingSystem, BeamTypeName = IsBeamTarget ? SelectedTargetTypeName : null, PipingSystemTypeName = IsBeamTarget ? null : SelectedTargetTypeName, PipeTypeName = PipeTypeName, LevelName = LevelName, AlignTangent = AlignTangent };
        CloseAction?.Invoke();
    }
}
