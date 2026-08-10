using System.Collections.ObjectModel;
using System.Windows.Input;
using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Application.UseCases.Gis;
using DHBIMWATER.Core.Gis;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;
using DHBIMWATER.UI.Views.Modeling;

namespace DHBIMWATER.UI.ViewModels.Modeling;

/// <summary>데이터 그리드를 분리해 보여주기 위한 소스 파일 구분. DXF는 DWG와 동일하게(레이어·XDATA 기반) 취급한다.</summary>
public enum PipeAlignmentSourceKind { Shp, Dwg, Excel }

/// <summary>DWG·DXF 파일 하나에 포함된 레이어의 다중선택 항목.</summary>
public sealed class CadLayerOption : ViewModelBase
{
    private bool _isSelected;
    public required string Name { get; init; }
    public bool IsSelected { get => _isSelected; set { if (SetProperty(ref _isSelected, value)) SelectionChanged?.Invoke(); } }
    public Action? SelectionChanged { get; init; }
}

public sealed class PipeAlignmentSourceFileItem : ViewModelBase
{
    /// <summary>직경·관종 필드 콤보박스에서 "필드 대신 수동 입력값을 쓰겠다"를 뜻하는 항목. 실제 필드명과 겹치지 않도록 꺾쇠로 감싼다.</summary>
    public const string ManualFieldOption = "<수동>";
    private string _pipeKind = string.Empty;
    private string? _diameterField, _kindField;
    private double? _manualDiameterMm;
    public required string Path { get; init; }
    public required ShapefileReadResult ReadResult { get; init; }
    /// <summary>엑셀 소스일 때만 값이 있다. 같은 파일이라도 시트별로 별도 항목이 되므로 파일 경로만으로는 구분되지 않는다.</summary>
    public string? SheetName { get; init; }
    public ExcelAlignmentMapping? ExcelMapping { get; init; }
    public PipeAlignmentSourceKind SourceKind => SheetName is not null ? PipeAlignmentSourceKind.Excel
        : string.Equals(System.IO.Path.GetExtension(Path), ".shp", StringComparison.OrdinalIgnoreCase) ? PipeAlignmentSourceKind.Shp
        : PipeAlignmentSourceKind.Dwg;
    public string FileName => SheetName is null ? System.IO.Path.GetFileName(Path) : $"{System.IO.Path.GetFileName(Path)} [시트: {SheetName}]";
    public string PipeKind { get => _pipeKind; set => SetProperty(ref _pipeKind, value); }
    public string? DiameterField { get => _diameterField; set { if (SetProperty(ref _diameterField, value)) MappingChanged?.Invoke(); } }
    public string? KindField { get => _kindField; set { if (SetProperty(ref _kindField, value)) MappingChanged?.Invoke(); } }
    /// <summary>필드 매핑이 없는 엑셀 소스처럼, 직경 필드로 해석할 수 없을 때 대신 쓰는 수동 입력값(mm).</summary>
    public double? ManualDiameterMm { get => _manualDiameterMm; set { if (SetProperty(ref _manualDiameterMm, value)) MappingChanged?.Invoke(); } }
    public IReadOnlyList<string> Fields => ReadResult.Fields.Select(x => x.Name).ToList();
    /// <summary>직경·관종 필드 콤보박스에 실제 표시하는 목록. 필드가 없거나(엑셀 미매핑 등) 있어도 항상 수동 옵션을 함께 보여준다.</summary>
    public IReadOnlyList<string> FieldOptions => new[] { ManualFieldOption }.Concat(Fields).ToList();
    public IReadOnlyList<string> Layers => ReadResult.Features.Select(x => x.Attributes.TryGetValue("LAYER", out var layer) ? layer : string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
    /// <summary>DWG·DXF의 폴리선 레이어를 파일별로 독립 선택한다. 최초에는 모든 레이어를 선택한다.</summary>
    public ObservableCollection<CadLayerOption> LayerOptions { get; } = new();
    public IReadOnlyList<string>? SelectedLayers => LayerOptions.Count == 0 ? null : LayerOptions.Where(x => x.IsSelected).Select(x => x.Name).ToList();
    public void InitializeLayerOptions()
    {
        foreach (var layer in Layers)
            LayerOptions.Add(new CadLayerOption { Name = layer, IsSelected = true, SelectionChanged = () => MappingChanged?.Invoke() });
    }
    public string Summary => $"레코드 {ReadResult.RecordCount:N0} · 정점 {ReadResult.VertexCount:N0} · 구간 {ReadResult.SegmentCount:N0}";
    public Action? MappingChanged { get; init; }
}

/// <summary>관로 선형 입력, 출력 모드, 곡관 설정과 사전 진단을 하나의 창에서 관리한다.</summary>
public sealed class PipeAlignmentModelingViewModel : ViewModelBase
{
    private readonly IFileDialogService _fileDialog; private readonly IDialogService _dialog;
    private readonly IReadOnlyList<IAlignmentSourceReader> _readers; private readonly AnalyzePipeNetworkUseCase _analyze;
    private readonly IBendSettingsRepo _settingsRepo; private readonly SaveBendSettingsUseCase _save; private readonly IExcelAlignmentSourceReader _excelReader;
    private readonly IProjectLocationQueryRepo _projectLocationQuery; private readonly IElementTypeQueryRepo _typeRepo;
    private PipeAlignmentSourceFileItem? _selectedFile; private PipeAlignmentOutputMode _outputMode;
    private double _referenceX, _referenceY, _intervalM = 6, _snapToleranceMm = 10;
    private bool _applySharedCoordinates, _alignTangent = true; private ZDatum _zDatum; private BendForm _form = BendForm.AType;
    private string? _beamTypeName, _pipingSystemTypeName, _pipeTypeName, _levelName, _summary, _settingsNotice;
    private string? _beamDiameterParameterName, _beamKindParameterName;
    private IReadOnlyList<string> _beamParameterNames = Array.Empty<string>();

    public PipeAlignmentModelingViewModel(IFileDialogService fileDialog, IDialogService dialog, IEnumerable<IAlignmentSourceReader> readers,
        IElementTypeQueryRepo typeRepo, ILevelQueryRepo levelRepo, AnalyzePipeNetworkUseCase analyze, IBendSettingsRepo settingsRepo, SaveBendSettingsUseCase save, IExcelAlignmentSourceReader excelReader, IProjectLocationQueryRepo projectLocationQuery)
    {
        _fileDialog = fileDialog; _dialog = dialog; _readers = readers.ToList(); _analyze = analyze; _settingsRepo = settingsRepo; _save = save; _excelReader = excelReader; _projectLocationQuery = projectLocationQuery; _typeRepo = typeRepo;
        BeamTypeNames = typeRepo.GetBeamTypeNames().ToList(); PipingSystemTypeNames = typeRepo.GetPipingSystemTypeNames().ToList(); PipeTypeNames = typeRepo.GetPipeTypeNames().ToList(); LevelNames = levelRepo.GetExistingLevelNames().ToList();
        _beamTypeName = BeamTypeNames.FirstOrDefault(); _pipingSystemTypeName = PipingSystemTypeNames.FirstOrDefault(); _pipeTypeName = PipeTypeNames.FirstOrDefault(); _levelName = LevelNames.FirstOrDefault();
        UpdateBeamParameterOptions();
        AddCommand = new RelayCommand(_ => AddFile()); RemoveCommand = new RelayCommand(_ => RemoveFile()); RunCommand = new RelayCommand(_ => RunDiagnosis()); CreateCommand = new RelayCommand(_ => RequestModeling()); CancelCommand = new RelayCommand(_ => CloseAction?.Invoke());
        SaveCommand = new RelayCommand(_ => SaveSettings()); AddToleranceRowCommand = new RelayCommand(_ => Tolerances.Add(new BendToleranceRow())); RemoveToleranceRowCommand = new RelayCommand(_ => { if (SelectedTolerance is not null) Tolerances.Remove(SelectedTolerance); }); AddFittingRowCommand = new RelayCommand(_ => Fittings.Add(new BendFittingRow())); RemoveFittingRowCommand = new RelayCommand(_ => { if (SelectedFitting is not null) Fittings.Remove(SelectedFitting); }); LoadSettings();
        Files.CollectionChanged += (_, _) => NotifyFilesByKindChanged();
    }
    public ObservableCollection<PipeAlignmentSourceFileItem> Files { get; } = new();
    // DataGrid 셀 편집(EditItem)은 IList를 지원하는 컬렉션 뷰가 필요하다. 지연 평가 IEnumerable(Where만 적용)을 그대로
    // 바인딩하면 EnumerableCollectionView로 감싸져 편집을 지원하지 않아 "EditItem을 사용할 수 없습니다" 예외가 난다.
    public IEnumerable<PipeAlignmentSourceFileItem> ShpFiles => Files.Where(x => x.SourceKind == PipeAlignmentSourceKind.Shp).ToList();
    public IEnumerable<PipeAlignmentSourceFileItem> DwgFiles => Files.Where(x => x.SourceKind == PipeAlignmentSourceKind.Dwg).ToList();
    public IEnumerable<PipeAlignmentSourceFileItem> ExcelFiles => Files.Where(x => x.SourceKind == PipeAlignmentSourceKind.Excel).ToList();
    public bool HasShpFiles => ShpFiles.Any(); public bool HasDwgFiles => DwgFiles.Any(); public bool HasExcelFiles => ExcelFiles.Any();
    private void NotifyFilesByKindChanged() { OnPropertyChanged(nameof(ShpFiles)); OnPropertyChanged(nameof(DwgFiles)); OnPropertyChanged(nameof(ExcelFiles)); OnPropertyChanged(nameof(HasShpFiles)); OnPropertyChanged(nameof(HasDwgFiles)); OnPropertyChanged(nameof(HasExcelFiles)); }
    public ObservableCollection<BendToleranceRow> Tolerances { get; } = new(); public ObservableCollection<BendFittingRow> Fittings { get; } = new(); public ObservableCollection<PipeNetworkNodeReport> Attention { get; } = new();
    public IReadOnlyList<string> BeamTypeNames { get; } public IReadOnlyList<string> PipingSystemTypeNames { get; } public IReadOnlyList<string> PipeTypeNames { get; } public IReadOnlyList<string> LevelNames { get; }
    public PipeAlignmentSourceFileItem? SelectedFile { get => _selectedFile; set { if (SetProperty(ref _selectedFile, value)) { OnPropertyChanged(nameof(SelectedFileDetails)); OnPropertyChanged(nameof(ProjectionDetails)); OnPropertyChanged(nameof(IsSelectedFileDwg)); } } }
    /// <summary>레이어 선택 입력란은 DWG·DXF 소스에서만 의미가 있다.</summary>
    public bool IsSelectedFileDwg => SelectedFile?.SourceKind == PipeAlignmentSourceKind.Dwg;
    public PipeAlignmentOutputMode OutputMode { get => _outputMode; set { if (SetProperty(ref _outputMode, value)) { OnPropertyChanged(nameof(IsDirectShape)); OnPropertyChanged(nameof(IsBeam)); OnPropertyChanged(nameof(IsPiping)); } } }
    public bool IsDirectShape { get => OutputMode == PipeAlignmentOutputMode.DirectShape; set { if (value) OutputMode = PipeAlignmentOutputMode.DirectShape; } }
    public bool IsBeam { get => OutputMode == PipeAlignmentOutputMode.Beam; set { if (value) OutputMode = PipeAlignmentOutputMode.Beam; } }
    public bool IsPiping { get => OutputMode == PipeAlignmentOutputMode.PipingSystem; set { if (value) OutputMode = PipeAlignmentOutputMode.PipingSystem; } }
    public double ReferenceX { get => _referenceX; set => SetProperty(ref _referenceX, value); } public double ReferenceY { get => _referenceY; set => SetProperty(ref _referenceY, value); }
    public bool ApplySharedCoordinates { get => _applySharedCoordinates; set => SetProperty(ref _applySharedCoordinates, value); } public ZDatum ZDatum { get => _zDatum; set => SetProperty(ref _zDatum, value); } public Array ZDatums => Enum.GetValues(typeof(ZDatum));
    public double IntervalM { get => _intervalM; set => SetProperty(ref _intervalM, value); } public bool AlignTangent { get => _alignTangent; set => SetProperty(ref _alignTangent, value); }
    public string? BeamTypeName { get => _beamTypeName; set { if (SetProperty(ref _beamTypeName, value)) UpdateBeamParameterOptions(); } } public string? PipingSystemTypeName { get => _pipingSystemTypeName; set => SetProperty(ref _pipingSystemTypeName, value); } public string? PipeTypeName { get => _pipeTypeName; set => SetProperty(ref _pipeTypeName, value); } public string? LevelName { get => _levelName; set => SetProperty(ref _levelName, value); }
    /// <summary>직경·관종 필드 매핑 콤보박스에서 "매핑하지 않음"을 뜻하는 항목.</summary>
    public const string ManualParameterOption = "<사용 안 함>";
    /// <summary>선택된 빔 유형에서 조회한 인스턴스 파라미터명 + 수동 옵션. 빔 유형이 바뀔 때마다 다시 조회한다.</summary>
    public IReadOnlyList<string> BeamParameterOptions => new[] { ManualParameterOption }.Concat(_beamParameterNames).ToList();
    /// <summary>직경(mm)을 기록할 빔 파라미터명. 빔 유형 선택 시 후보 목록에서 자동 매칭을 시도하고, 실패하면 수동 옵션이 선택된다.</summary>
    public string? BeamDiameterParameterName { get => _beamDiameterParameterName; set => SetProperty(ref _beamDiameterParameterName, value); }
    /// <summary>관종을 기록할 빔 파라미터명. 자동 매칭 규칙은 <see cref="BeamDiameterParameterName"/>과 같다.</summary>
    public string? BeamKindParameterName { get => _beamKindParameterName; set => SetProperty(ref _beamKindParameterName, value); }
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

    private void AddFile() { var path = _fileDialog.OpenFile("관로 선형 파일 선택", "관로 선형 파일 (*.shp;*.dxf;*.dwg;*.xlsx)|*.shp;*.dxf;*.dwg;*.xlsx|모든 파일 (*.*)|*.*"); if (string.IsNullOrWhiteSpace(path)) return;
        if (string.Equals(System.IO.Path.GetExtension(path), ".xlsx", StringComparison.OrdinalIgnoreCase)) { AddExcelFile(path); return; }
        if (Files.Any(x => string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase))) return;
        try { var reader = _readers.FirstOrDefault(x => x.CanRead(path)) ?? throw new InvalidOperationException($"'{path}' 파일을 읽을 수 있는 리더가 없습니다."); var read = reader.Read(path); var item = new PipeAlignmentSourceFileItem { Path = path, ReadResult = read, MappingChanged = NotifyFileMappingChanged }; item.InitializeLayerOptions(); item.DiameterField = AlignmentAttributeParser.GuessDiameterField(read.Fields.Select(x => x.Name)) ?? PipeAlignmentSourceFileItem.ManualFieldOption; item.KindField = AlignmentAttributeParser.GuessKindField(read.Fields.Select(x => x.Name)) ?? PipeAlignmentSourceFileItem.ManualFieldOption; Files.Add(item); SelectedFile = item; RefreshReferencePoint(); NotifyFileMappingChanged();
        // .prj/.cpg 누락과 SHP·DBF 레코드 수 불일치는 파일을 추가한 시점에 알아야 하는 정보다.
        // 예전에는 read.Warnings를 버리고 모델링 실행 시점에야 노출했다.
        if (read.Warnings.Count > 0) _dialog.Info("파일 확인", $"{System.IO.Path.GetFileName(path)}\n\n{string.Join("\n", read.Warnings)}");
        } catch (Exception ex) { _dialog.Warn("파일 읽기 실패", ex.Message); } }
    private void AddExcelFile(string path)
    {
        ExcelAlignmentMappingViewModel mappingVm;
        try { mappingVm = new ExcelAlignmentMappingViewModel(_excelReader, _dialog, path); }
        catch (Exception ex) { _dialog.Warn("엑셀 읽기 실패", ex.Message); return; }
        var dlg = new ExcelAlignmentMappingView(mappingVm);
        var owner = System.Windows.Application.Current.Windows.OfType<System.Windows.Window>().FirstOrDefault(x => x is PipeAlignmentModelingView);
        if (owner is not null) dlg.Owner = owner;
        dlg.ShowDialog();
        if (owner is not null) owner.Activate();
        if (mappingVm.Result is not { Count: > 0 } mappings) return;
        foreach (var mapping in mappings)
        {
            if (Files.Any(x => string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase) && string.Equals(x.SheetName, mapping.SheetName, StringComparison.Ordinal))) continue;
            try
            {
                var read = _excelReader.Read(path, mapping);
                var item = new PipeAlignmentSourceFileItem { Path = path, SheetName = mapping.SheetName, ExcelMapping = mapping, ReadResult = read, MappingChanged = NotifyFileMappingChanged };
                item.DiameterField = mapping.DiameterColumnIndex is not null ? "직경" : PipeAlignmentSourceFileItem.ManualFieldOption;
                item.KindField = mapping.KindColumnIndex is not null ? "관종" : PipeAlignmentSourceFileItem.ManualFieldOption;
                Files.Add(item); SelectedFile = item;
                if (read.Warnings.Count > 0) _dialog.Info("파일 확인", $"{item.FileName}\n\n{string.Join("\n", read.Warnings)}");
            }
            catch (Exception ex) { _dialog.Warn("엑셀 읽기 실패", $"{mapping.SheetName}: {ex.Message}"); }
        }
        RefreshReferencePoint(); NotifyFileMappingChanged();
    }
    private void RemoveFile() { if (SelectedFile is null) return; Files.Remove(SelectedFile); SelectedFile = Files.FirstOrDefault(); RefreshReferencePoint(); NotifyFileMappingChanged(); }
    private void RefreshReferencePoint() { var point = AlignmentReferencePoint.FromFirstVertex(Files.SelectMany(x => x.ReadResult.Features)); ReferenceX = point?.X ?? 0; ReferenceY = point?.Y ?? 0; }
    private static string? ResolvedField(string? field) => field is null or PipeAlignmentSourceFileItem.ManualFieldOption ? null : field;
    private static string? ResolvedParameter(string? name) => name is null or ManualParameterOption ? null : name;
    /// <summary>빔 유형에서 조회한 파라미터명으로 직경·관종 후보 매칭을 다시 시도한다. 매칭되면 자동 선택, 안 되면 수동 옵션으로 남긴다.</summary>
    private void UpdateBeamParameterOptions()
    {
        _beamParameterNames = string.IsNullOrWhiteSpace(BeamTypeName) ? Array.Empty<string>() : _typeRepo.GetBeamInstanceParameterNames(BeamTypeName).ToList();
        OnPropertyChanged(nameof(BeamParameterOptions));
        BeamDiameterParameterName = FamilyParameterMapper.GuessDiameterParameter(_beamParameterNames) ?? ManualParameterOption;
        BeamKindParameterName = FamilyParameterMapper.GuessKindParameter(_beamParameterNames) ?? ManualParameterOption;
    }
    private IReadOnlyList<AlignmentSourceFile> SourceFiles() => Files.Select(x => new AlignmentSourceFile(x.Path, x.PipeKind, ResolvedField(x.DiameterField), ResolvedField(x.KindField), x.SelectedLayers, x.ExcelMapping, x.ManualDiameterMm)).ToList();
    private int CountUnresolved() { var count = 0; foreach (var item in Files) { if (item.ManualDiameterMm is > 0) continue; var diameterField = ResolvedField(item.DiameterField); foreach (var feature in item.ReadResult.Features) { if (diameterField is null || !feature.Attributes.TryGetValue(diameterField, out var value) || !AlignmentAttributeParser.ParseDiameter(value).Success) count++; } } return count; }
    private void NotifyFileMappingChanged() { OnPropertyChanged(nameof(DiameterUnresolvedCount)); OnPropertyChanged(nameof(HasDiameterUnresolved)); }
    private PipeNetworkDiagnosisResult? RunDiagnosis(bool showWarnings = true) { if (Files.Count == 0) { _dialog.Warn("입력 확인", "하나 이상의 SHP, DXF 또는 DWG 파일을 추가하세요."); return null; } if (SnapToleranceMm <= 0) { _dialog.Warn("입력 확인", "스냅 허용오차는 0보다 커야 합니다."); return null; } try { var result = _analyze.Execute(new PipeNetworkDiagnosisRequest { Files = SourceFiles(), SnapToleranceMm = SnapToleranceMm, Form = Form }); Attention.Clear(); foreach (var report in result.Attention) Attention.Add(report); Summary = $"절점 {result.NodeCount} / 간선 {result.EdgeCount}\n{string.Join(", ", result.KindCounts.OrderBy(x => x.Key).Select(x => $"{x.Key} {x.Value}"))}\n곡관 판정 — 표준 {result.BendStandardCount}, 생략 {result.BendNoneCount}, 미해결 {result.BendUnresolvedCount}\n경고 {result.Warnings.Count}건"; if (showWarnings && result.Warnings.Count > 0) _dialog.Info("진단 경고", string.Join("\n", result.Warnings.Take(20)) + (result.Warnings.Count > 20 ? $"\n… 외 {result.Warnings.Count - 20}건" : string.Empty)); return result; } catch (Exception ex) { _dialog.Warn("관로 네트워크 진단", $"진단에 실패했습니다.\n{ex.Message}"); return null; } }
    private void RequestModeling() { if (Files.Count == 0) { _dialog.Warn("입력 확인", "하나 이상의 SHP, DXF 또는 DWG 파일을 추가하세요."); return; } if (OutputMode is PipeAlignmentOutputMode.Beam or PipeAlignmentOutputMode.PipingSystem && IntervalM <= 0) { _dialog.Warn("입력 확인", "배치 간격은 0보다 커야 합니다."); return; } var diagnosis = RunDiagnosis(false); if (diagnosis is null) return; if ((diagnosis.BendUnresolvedCount > 0 || diagnosis.SizeConflictCount > 0 || diagnosis.Warnings.Any(x => x.Contains("직관이 들어갈 자리가 없습니다"))) && !_dialog.Confirm("사전 진단 경고", $"미해결 곡관 {diagnosis.BendUnresolvedCount}건, 치수 모순 {diagnosis.SizeConflictCount}건을 확인했습니다. 계속 모델링할까요?")) return; ConfirmSharedCoordinates(); RequestedModeling = new PipeAlignmentModelingRequest { Files = SourceFiles(), ReferenceX = ReferenceX, ReferenceY = ReferenceY, ApplySharedCoordinates = ApplySharedCoordinates, ZDatum = ZDatum, OutputMode = OutputMode, IntervalMm = IntervalM * 1000, BeamTypeName = BeamTypeName, BeamDiameterParameterName = ResolvedParameter(BeamDiameterParameterName), BeamKindParameterName = ResolvedParameter(BeamKindParameterName), PipingSystemTypeName = PipingSystemTypeName, PipeTypeName = PipeTypeName, LevelName = LevelName, AlignTangent = AlignTangent, SnapToleranceMm = SnapToleranceMm, Form = Form }; CloseAction?.Invoke(); }
    /// <summary>공유좌표 미적용 상태에서 PBP 공유좌표가 (0,0)이면 실제 위치에서 멀리 떨어져 모델링된다는 점을 알리고, 승낙 시 체크박스를 켠다.</summary>
    private void ConfirmSharedCoordinates()
    {
        if (ApplySharedCoordinates) return;
        try
        {
            var (eastWest, northSouth) = _projectLocationQuery.GetProjectBasePointSharedPosition();
            if (Math.Abs(eastWest) < 1e-4 && Math.Abs(northSouth) < 1e-4
                && _dialog.Confirm("공유좌표 확인", "현재 프로젝트 기준점의 공유좌표가 (0, 0)으로 지정되어 있어, 모델이 실제 위치에서 멀리 떨어져 모델링됩니다.\n프로젝트 공유좌표에도 기준점을 적용하시겠습니까?"))
                ApplySharedCoordinates = true;
        }
        catch { /* PBP 조회에 실패해도 모델링 자체는 막지 않는다 */ }
    }
    private void LoadSettings() { var stored = _settingsRepo.Load(); var settings = stored ?? BendSettings.Default; SettingsNotice = stored is null ? "저장된 설정이 없어 기본값(미저장)을 표시합니다. 곡관 치수는 제조사 실측치라 기본값이 없습니다." : string.Empty; foreach (var e in settings.Tolerance.Entries) Tolerances.Add(new BendToleranceRow { PipeKind = e.PipeKind, DiameterMm = e.DiameterMm, ToleranceDeg = e.ToleranceDeg }); foreach (var e in settings.Fittings.Entries) Fittings.Add(new BendFittingRow { PipeKind = e.PipeKind, DiameterMm = e.DiameterMm, AngleDeg = e.AngleDeg, Form = e.Form, LayingLengthMm = e.LayingLengthMm, CenterlineRadiusMm = e.CenterlineRadiusMm, ExtraLegLengthMm = e.ExtraLegLengthMm }); }
    private void SaveSettings() { var invalid = Fittings.Count(x => x.CenterlineRadiusMm > 0 && x.LayingLengthMm < x.TangentLengthMm); if (invalid > 0) _dialog.Warn("곡관 치수 확인", $"t가 접선길이 T보다 작은 행이 {invalid}건 있습니다.\n호가 곡관 몸통 밖으로 나가는 치수이며, 저장과 모델링은 그대로 진행합니다."); try { _save.Execute(new BendSettings(new BendToleranceTable(Tolerances.Select(x => new BendToleranceEntry(x.PipeKind, x.DiameterMm, x.ToleranceDeg)).ToList()), new BendFittingCatalog(Fittings.Select(x => new BendFittingEntry(x.PipeKind, x.DiameterMm, x.AngleDeg, x.Form, x.LayingLengthMm, x.CenterlineRadiusMm, x.ExtraLegLengthMm)).ToList()))); SettingsNotice = string.Empty; _dialog.Info("곡관 설정", "허용굴곡과 곡관 치수를 저장했습니다."); } catch (Exception ex) { _dialog.Warn("곡관 설정", $"저장에 실패했습니다.\n{ex.Message}"); } }
}
