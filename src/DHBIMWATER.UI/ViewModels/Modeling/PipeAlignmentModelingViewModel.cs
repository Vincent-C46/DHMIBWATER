using System.Collections.ObjectModel;
using System.Windows.Input;
using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Application.UseCases.Gis;
using DHBIMWATER.Core.Gis;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;

namespace DHBIMWATER.UI.ViewModels.Modeling;

public sealed class PipeAlignmentSourceFileItem : ViewModelBase
{
    private string _pipeKind = string.Empty;
    private string? _diameterField, _kindField, _selectedLayersText = string.Empty;
    public required string Path { get; init; }
    public required ShapefileReadResult ReadResult { get; init; }
    public string FileName => System.IO.Path.GetFileName(Path);
    public string PipeKind { get => _pipeKind; set => SetProperty(ref _pipeKind, value); }
    public string? DiameterField { get => _diameterField; set { if (SetProperty(ref _diameterField, value)) MappingChanged?.Invoke(); } }
    public string? KindField { get => _kindField; set { if (SetProperty(ref _kindField, value)) MappingChanged?.Invoke(); } }
    public IReadOnlyList<string> Fields => ReadResult.Fields.Select(x => x.Name).ToList();
    public IReadOnlyList<string> Layers => ReadResult.Features.Select(x => x.Attributes.TryGetValue("LAYER", out var layer) ? layer : string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
    /// <summary>쉼표로 구분한 레이어명. 비우면 모든 레이어를 사용한다.</summary>
    public string SelectedLayersText { get => _selectedLayersText; set { if (SetProperty(ref _selectedLayersText, value)) MappingChanged?.Invoke(); } }
    public IReadOnlyList<string>? SelectedLayers => string.IsNullOrWhiteSpace(SelectedLayersText) ? null : SelectedLayersText.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    public string Summary => $"레코드 {ReadResult.RecordCount:N0} · 정점 {ReadResult.VertexCount:N0} · 구간 {ReadResult.SegmentCount:N0}";
    public Action? MappingChanged { get; init; }
}

/// <summary>관로 선형 입력, 출력 모드, 곡관 설정과 사전 진단을 하나의 창에서 관리한다.</summary>
public sealed class PipeAlignmentModelingViewModel : ViewModelBase
{
    private readonly IFileDialogService _fileDialog; private readonly IDialogService _dialog;
    private readonly IReadOnlyList<IAlignmentSourceReader> _readers; private readonly AnalyzePipeNetworkUseCase _analyze;
    private readonly IBendSettingsRepo _settingsRepo; private readonly SaveBendSettingsUseCase _save;
    private PipeAlignmentSourceFileItem? _selectedFile; private PipeAlignmentOutputMode _outputMode;
    private double _referenceX, _referenceY, _intervalM = 6, _snapToleranceMm = 10;
    private bool _applySharedCoordinates, _alignTangent = true; private ZDatum _zDatum; private BendForm _form = BendForm.AType;
    private string? _beamTypeName, _pipingSystemTypeName, _pipeTypeName, _levelName, _summary, _settingsNotice;

    public PipeAlignmentModelingViewModel(IFileDialogService fileDialog, IDialogService dialog, IEnumerable<IAlignmentSourceReader> readers,
        IElementTypeQueryRepo typeRepo, ILevelQueryRepo levelRepo, AnalyzePipeNetworkUseCase analyze, IBendSettingsRepo settingsRepo, SaveBendSettingsUseCase save)
    {
        _fileDialog = fileDialog; _dialog = dialog; _readers = readers.ToList(); _analyze = analyze; _settingsRepo = settingsRepo; _save = save;
        BeamTypeNames = typeRepo.GetBeamTypeNames().ToList(); PipingSystemTypeNames = typeRepo.GetPipingSystemTypeNames().ToList(); PipeTypeNames = typeRepo.GetPipeTypeNames().ToList(); LevelNames = levelRepo.GetExistingLevelNames().ToList();
        _beamTypeName = BeamTypeNames.FirstOrDefault(); _pipingSystemTypeName = PipingSystemTypeNames.FirstOrDefault(); _pipeTypeName = PipeTypeNames.FirstOrDefault(); _levelName = LevelNames.FirstOrDefault();
        AddCommand = new RelayCommand(_ => AddFile()); RemoveCommand = new RelayCommand(_ => RemoveFile()); RunCommand = new RelayCommand(_ => RunDiagnosis()); CreateCommand = new RelayCommand(_ => RequestModeling()); CancelCommand = new RelayCommand(_ => CloseAction?.Invoke());
        SaveCommand = new RelayCommand(_ => SaveSettings()); AddToleranceRowCommand = new RelayCommand(_ => Tolerances.Add(new BendToleranceRow())); RemoveToleranceRowCommand = new RelayCommand(_ => { if (SelectedTolerance is not null) Tolerances.Remove(SelectedTolerance); }); AddFittingRowCommand = new RelayCommand(_ => Fittings.Add(new BendFittingRow())); RemoveFittingRowCommand = new RelayCommand(_ => { if (SelectedFitting is not null) Fittings.Remove(SelectedFitting); }); LoadSettings();
    }
    public ObservableCollection<PipeAlignmentSourceFileItem> Files { get; } = new();
    public ObservableCollection<BendToleranceRow> Tolerances { get; } = new(); public ObservableCollection<BendFittingRow> Fittings { get; } = new(); public ObservableCollection<PipeNetworkNodeReport> Attention { get; } = new();
    public IReadOnlyList<string> BeamTypeNames { get; } public IReadOnlyList<string> PipingSystemTypeNames { get; } public IReadOnlyList<string> PipeTypeNames { get; } public IReadOnlyList<string> LevelNames { get; }
    public PipeAlignmentSourceFileItem? SelectedFile { get => _selectedFile; set { if (SetProperty(ref _selectedFile, value)) { OnPropertyChanged(nameof(SelectedFileDetails)); OnPropertyChanged(nameof(ProjectionDetails)); } } }
    public PipeAlignmentOutputMode OutputMode { get => _outputMode; set { if (SetProperty(ref _outputMode, value)) { OnPropertyChanged(nameof(IsDirectShape)); OnPropertyChanged(nameof(IsBeam)); OnPropertyChanged(nameof(IsPiping)); } } }
    public bool IsDirectShape { get => OutputMode == PipeAlignmentOutputMode.DirectShape; set { if (value) OutputMode = PipeAlignmentOutputMode.DirectShape; } }
    public bool IsBeam { get => OutputMode == PipeAlignmentOutputMode.Beam; set { if (value) OutputMode = PipeAlignmentOutputMode.Beam; } }
    public bool IsPiping { get => OutputMode == PipeAlignmentOutputMode.PipingSystem; set { if (value) OutputMode = PipeAlignmentOutputMode.PipingSystem; } }
    public double ReferenceX { get => _referenceX; set => SetProperty(ref _referenceX, value); } public double ReferenceY { get => _referenceY; set => SetProperty(ref _referenceY, value); }
    public bool ApplySharedCoordinates { get => _applySharedCoordinates; set => SetProperty(ref _applySharedCoordinates, value); } public ZDatum ZDatum { get => _zDatum; set => SetProperty(ref _zDatum, value); } public Array ZDatums => Enum.GetValues(typeof(ZDatum));
    public double IntervalM { get => _intervalM; set => SetProperty(ref _intervalM, value); } public bool AlignTangent { get => _alignTangent; set => SetProperty(ref _alignTangent, value); }
    public string? BeamTypeName { get => _beamTypeName; set => SetProperty(ref _beamTypeName, value); } public string? PipingSystemTypeName { get => _pipingSystemTypeName; set => SetProperty(ref _pipingSystemTypeName, value); } public string? PipeTypeName { get => _pipeTypeName; set => SetProperty(ref _pipeTypeName, value); } public string? LevelName { get => _levelName; set => SetProperty(ref _levelName, value); }
    public double SnapToleranceMm { get => _snapToleranceMm; set => SetProperty(ref _snapToleranceMm, value); } public BendForm Form { get => _form; set => SetProperty(ref _form, value); } public IEnumerable<BendForm> Forms => Enum.GetValues<BendForm>();
    public BendToleranceRow? SelectedTolerance { get; set; } public BendFittingRow? SelectedFitting { get; set; }
    public string Summary { get => _summary ?? string.Empty; private set => SetProperty(ref _summary, value); } public string SettingsNotice { get => _settingsNotice ?? string.Empty; private set => SetProperty(ref _settingsNotice, value); }
    public string SelectedFileDetails => SelectedFile is null ? "파일을 추가하면 DBF/XDATA 필드와 샘플 속성이 표시됩니다." : $"{SelectedFile.Summary}\n필드: {string.Join(", ", SelectedFile.Fields)}\n레이어: {string.Join(", ", SelectedFile.Layers)}\n샘플: {string.Join(", ", (SelectedFile.ReadResult.SampleAttributes ?? new Dictionary<string, string>()).Select(x => $"{x.Key}={x.Value}"))}\n인코딩: {SelectedFile.ReadResult.EncodingName}";
    // WKT가 비는 원인은 두 가지다 — DXF라서 애초에 좌표계가 없는 경우와, SHP인데 형제 .prj가 없는 경우.
    // 둘을 같은 문구로 뭉치면 .prj 누락을 놓치므로 확장자로 분기한다.
    public string ProjectionDetails => SelectedFile is null ? string.Empty
        : !string.IsNullOrWhiteSpace(SelectedFile.ReadResult.ProjectionWkt) ? $"추천: {SelectedFile.ReadResult.RecommendedEpsg ?? "없음"}\n{SelectedFile.ReadResult.ProjectionWkt}"
        : System.IO.Path.GetExtension(SelectedFile.Path).Equals(".shp", StringComparison.OrdinalIgnoreCase) ? "⚠ 형제 .prj 파일이 없어 좌표계를 확인할 수 없습니다. 좌표값이 어느 좌표계인지 직접 확인하세요."
        : $"좌표계 정보 없음 ({System.IO.Path.GetExtension(SelectedFile.Path).TrimStart('.').ToUpperInvariant()})";
    public int DiameterUnresolvedCount => CountUnresolved(); public bool HasDiameterUnresolved => DiameterUnresolvedCount > 0;
    public ICommand AddCommand { get; } public ICommand RemoveCommand { get; } public ICommand RunCommand { get; } public ICommand CreateCommand { get; } public ICommand CancelCommand { get; } public ICommand SaveCommand { get; } public ICommand AddToleranceRowCommand { get; } public ICommand RemoveToleranceRowCommand { get; } public ICommand AddFittingRowCommand { get; } public ICommand RemoveFittingRowCommand { get; }
    public Action? CloseAction { get; set; } public PipeAlignmentModelingRequest? RequestedModeling { get; private set; }

    private void AddFile() { var path = _fileDialog.OpenFile("관로 선형 파일 선택", "관로 선형 파일 (*.shp;*.dxf;*.dwg)|*.shp;*.dxf;*.dwg|모든 파일 (*.*)|*.*"); if (string.IsNullOrWhiteSpace(path) || Files.Any(x => string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase))) return; try { var reader = _readers.FirstOrDefault(x => x.CanRead(path)) ?? throw new InvalidOperationException($"'{path}' 파일을 읽을 수 있는 리더가 없습니다."); var read = reader.Read(path); var item = new PipeAlignmentSourceFileItem { Path = path, ReadResult = read, MappingChanged = NotifyFileMappingChanged, SelectedLayersText = string.Join(", ", read.Features.Select(x => x.Attributes.TryGetValue("LAYER", out var layer) ? layer : string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase)) }; item.DiameterField = AlignmentAttributeParser.GuessDiameterField(read.Fields.Select(x => x.Name)); item.KindField = AlignmentAttributeParser.GuessKindField(read.Fields.Select(x => x.Name)); Files.Add(item); SelectedFile = item; RefreshReferencePoint(); NotifyFileMappingChanged();
        // .prj/.cpg 누락과 SHP·DBF 레코드 수 불일치는 파일을 추가한 시점에 알아야 하는 정보다.
        // 예전에는 read.Warnings를 버리고 모델링 실행 시점에야 노출했다.
        if (read.Warnings.Count > 0) _dialog.Info("파일 확인", $"{System.IO.Path.GetFileName(path)}\n\n{string.Join("\n", read.Warnings)}");
        } catch (Exception ex) { _dialog.Warn("파일 읽기 실패", ex.Message); } }
    private void RemoveFile() { if (SelectedFile is null) return; Files.Remove(SelectedFile); SelectedFile = Files.FirstOrDefault(); RefreshReferencePoint(); NotifyFileMappingChanged(); }
    private void RefreshReferencePoint() { var point = AlignmentReferencePoint.FromFirstVertex(Files.SelectMany(x => x.ReadResult.Features)); ReferenceX = point?.X ?? 0; ReferenceY = point?.Y ?? 0; }
    private IReadOnlyList<AlignmentSourceFile> SourceFiles() => Files.Select(x => new AlignmentSourceFile(x.Path, x.PipeKind, x.DiameterField, x.KindField, x.SelectedLayers)).ToList();
    private int CountUnresolved() { var count = 0; foreach (var item in Files) foreach (var feature in item.ReadResult.Features) { if (item.DiameterField is null || !feature.Attributes.TryGetValue(item.DiameterField, out var value) || !AlignmentAttributeParser.ParseDiameter(value).Success) count++; } return count; }
    private void NotifyFileMappingChanged() { OnPropertyChanged(nameof(DiameterUnresolvedCount)); OnPropertyChanged(nameof(HasDiameterUnresolved)); }
    private PipeNetworkDiagnosisResult? RunDiagnosis(bool showWarnings = true) { if (Files.Count == 0) { _dialog.Warn("입력 확인", "하나 이상의 SHP, DXF 또는 DWG 파일을 추가하세요."); return null; } if (SnapToleranceMm <= 0) { _dialog.Warn("입력 확인", "스냅 허용오차는 0보다 커야 합니다."); return null; } try { var result = _analyze.Execute(new PipeNetworkDiagnosisRequest { Files = SourceFiles(), SnapToleranceMm = SnapToleranceMm, Form = Form }); Attention.Clear(); foreach (var report in result.Attention) Attention.Add(report); Summary = $"절점 {result.NodeCount} / 간선 {result.EdgeCount}\n{string.Join(", ", result.KindCounts.OrderBy(x => x.Key).Select(x => $"{x.Key} {x.Value}"))}\n곡관 판정 — 표준 {result.BendStandardCount}, 생략 {result.BendNoneCount}, 미해결 {result.BendUnresolvedCount}\n경고 {result.Warnings.Count}건"; if (showWarnings && result.Warnings.Count > 0) _dialog.Info("진단 경고", string.Join("\n", result.Warnings.Take(20)) + (result.Warnings.Count > 20 ? $"\n… 외 {result.Warnings.Count - 20}건" : string.Empty)); return result; } catch (Exception ex) { _dialog.Warn("관로 네트워크 진단", $"진단에 실패했습니다.\n{ex.Message}"); return null; } }
    private void RequestModeling() { if (Files.Count == 0) { _dialog.Warn("입력 확인", "하나 이상의 SHP, DXF 또는 DWG 파일을 추가하세요."); return; } if (OutputMode is PipeAlignmentOutputMode.Beam or PipeAlignmentOutputMode.PipingSystem && IntervalM <= 0) { _dialog.Warn("입력 확인", "배치 간격은 0보다 커야 합니다."); return; } var diagnosis = RunDiagnosis(false); if (diagnosis is null) return; if ((diagnosis.BendUnresolvedCount > 0 || diagnosis.SizeConflictCount > 0 || diagnosis.Warnings.Any(x => x.Contains("직관이 들어갈 자리가 없습니다"))) && !_dialog.Confirm("사전 진단 경고", $"미해결 곡관 {diagnosis.BendUnresolvedCount}건, 치수 모순 {diagnosis.SizeConflictCount}건을 확인했습니다. 계속 모델링할까요?")) return; RequestedModeling = new PipeAlignmentModelingRequest { Files = SourceFiles(), ReferenceX = ReferenceX, ReferenceY = ReferenceY, ApplySharedCoordinates = ApplySharedCoordinates, ZDatum = ZDatum, OutputMode = OutputMode, IntervalMm = IntervalM * 1000, BeamTypeName = BeamTypeName, PipingSystemTypeName = PipingSystemTypeName, PipeTypeName = PipeTypeName, LevelName = LevelName, AlignTangent = AlignTangent }; CloseAction?.Invoke(); }
    private void LoadSettings() { var stored = _settingsRepo.Load(); var settings = stored ?? BendSettings.Default; SettingsNotice = stored is null ? "저장된 설정이 없어 기본값(미저장)을 표시합니다. 곡관 치수는 제조사 실측치라 기본값이 없습니다." : string.Empty; foreach (var e in settings.Tolerance.Entries) Tolerances.Add(new BendToleranceRow { PipeKind = e.PipeKind, DiameterMm = e.DiameterMm, ToleranceDeg = e.ToleranceDeg }); foreach (var e in settings.Fittings.Entries) Fittings.Add(new BendFittingRow { PipeKind = e.PipeKind, DiameterMm = e.DiameterMm, AngleDeg = e.AngleDeg, Form = e.Form, LayingLengthMm = e.LayingLengthMm, CenterlineRadiusMm = e.CenterlineRadiusMm }); }
    private void SaveSettings() { var invalid = Fittings.Count(x => x.CenterlineRadiusMm > 0 && x.LayingLengthMm < x.TangentLengthMm); if (invalid > 0) _dialog.Warn("곡관 치수 확인", $"t가 접선길이 T보다 작은 행이 {invalid}건 있습니다.\n호가 곡관 몸통 밖으로 나가는 치수이며, 저장과 모델링은 그대로 진행합니다."); try { _save.Execute(new BendSettings(new BendToleranceTable(Tolerances.Select(x => new BendToleranceEntry(x.PipeKind, x.DiameterMm, x.ToleranceDeg)).ToList()), new BendFittingCatalog(Fittings.Select(x => new BendFittingEntry(x.PipeKind, x.DiameterMm, x.AngleDeg, x.Form, x.LayingLengthMm, x.CenterlineRadiusMm)).ToList()))); SettingsNotice = string.Empty; _dialog.Info("곡관 설정", "허용굴곡과 곡관 치수를 저장했습니다."); } catch (Exception ex) { _dialog.Warn("곡관 설정", $"저장에 실패했습니다.\n{ex.Message}"); } }
}
