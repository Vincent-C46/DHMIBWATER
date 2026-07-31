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

/// <summary>허용굴곡 탭의 편집 행. 그리드에서 직접 고치므로 record가 아니라 알림 가능한 클래스다.</summary>
public sealed class BendToleranceRow : ViewModelBase
{
    private string _pipeKind = string.Empty;
    private double _diameterMm, _toleranceDeg;
    public string PipeKind { get => _pipeKind; set => SetProperty(ref _pipeKind, value); }
    public double DiameterMm { get => _diameterMm; set => SetProperty(ref _diameterMm, value); }
    public double ToleranceDeg { get => _toleranceDeg; set => SetProperty(ref _toleranceDeg, value); }
}

/// <summary>곡관 치수 탭의 편집 행. t와 R은 각각 입력하며 서로 유도하지 않는다.</summary>
public sealed class BendFittingRow : ViewModelBase
{
    private string _pipeKind = string.Empty;
    private double _diameterMm, _angleDeg = 45d, _layingLengthMm, _centerlineRadiusMm;
    private BendForm _form = BendForm.AType;
    public string PipeKind { get => _pipeKind; set => SetProperty(ref _pipeKind, value); }
    public double DiameterMm { get => _diameterMm; set => SetProperty(ref _diameterMm, value); }
    public double AngleDeg { get => _angleDeg; set => SetProperty(ref _angleDeg, value); }
    public BendForm Form { get => _form; set => SetProperty(ref _form, value); }
    /// <summary>t — 절점에서 곡관 끝단까지(mm).</summary>
    public double LayingLengthMm { get => _layingLengthMm; set => SetProperty(ref _layingLengthMm, value); }
    /// <summary>R — 중심선 호의 곡률반경(mm).</summary>
    public double CenterlineRadiusMm { get => _centerlineRadiusMm; set { if (SetProperty(ref _centerlineRadiusMm, value)) OnPropertyChanged(nameof(TangentLengthMm)); } }
    /// <summary>계산값 T = R·tan(θ/2). 입력값이 아니라 확인용이다.</summary>
    public double TangentLengthMm => Math.Round(BendResolver.TangentLength(CenterlineRadiusMm, AngleDeg), 1);
}

public sealed class PipeNetworkDiagnosisViewModel : ViewModelBase
{
    private readonly IFileDialogService _fileDialog;
    private readonly IDialogService _dialog;
    private readonly IBendSettingsRepo _settingsRepo;
    private readonly AnalyzePipeNetworkUseCase _analyze;
    private readonly SaveBendSettingsUseCase _save;

    private AlignmentPlacementFileItem? _selectedFile;
    private double _snapToleranceMm = 10d;
    private BendForm _form = BendForm.AType;
    private bool _parseCombinedDiameter = true;
    private string _summary = string.Empty;
    private string _settingsNotice = string.Empty;

    public PipeNetworkDiagnosisViewModel(
        IFileDialogService fileDialog,
        IDialogService dialog,
        IBendSettingsRepo settingsRepo,
        AnalyzePipeNetworkUseCase analyze,
        SaveBendSettingsUseCase save)
    {
        _fileDialog = fileDialog; _dialog = dialog; _settingsRepo = settingsRepo; _analyze = analyze; _save = save;
        AddCommand = new RelayCommand(_ => AddFile());
        RemoveCommand = new RelayCommand(_ => RemoveFile());
        RunCommand = new RelayCommand(_ => Run());
        SaveCommand = new RelayCommand(_ => SaveSettings());
        AddToleranceRowCommand = new RelayCommand(_ => Tolerances.Add(new BendToleranceRow()));
        RemoveToleranceRowCommand = new RelayCommand(_ => { if (SelectedTolerance is not null) Tolerances.Remove(SelectedTolerance); });
        AddFittingRowCommand = new RelayCommand(_ => Fittings.Add(new BendFittingRow()));
        RemoveFittingRowCommand = new RelayCommand(_ => { if (SelectedFitting is not null) Fittings.Remove(SelectedFitting); });
        CloseCommand = new RelayCommand(_ => CloseAction?.Invoke());
        LoadSettings();
    }

    public ObservableCollection<AlignmentPlacementFileItem> Files { get; } = new();
    public ObservableCollection<BendToleranceRow> Tolerances { get; } = new();
    public ObservableCollection<BendFittingRow> Fittings { get; } = new();
    public ObservableCollection<PipeNetworkNodeReport> Attention { get; } = new();

    public AlignmentPlacementFileItem? SelectedFile { get => _selectedFile; set => SetProperty(ref _selectedFile, value); }
    public BendToleranceRow? SelectedTolerance { get; set; }
    public BendFittingRow? SelectedFitting { get; set; }

    public double SnapToleranceMm { get => _snapToleranceMm; set => SetProperty(ref _snapToleranceMm, value); }
    public bool ParseCombinedDiameter { get => _parseCombinedDiameter; set => SetProperty(ref _parseCombinedDiameter, value); }
    public BendForm Form { get => _form; set => SetProperty(ref _form, value); }
    public IEnumerable<BendForm> Forms => Enum.GetValues<BendForm>();
    public IEnumerable<double> StandardAngles => BendToleranceTable.StandardAngles;

    public string Summary { get => _summary; private set => SetProperty(ref _summary, value); }
    public string SettingsNotice { get => _settingsNotice; private set => SetProperty(ref _settingsNotice, value); }

    public ICommand AddCommand { get; }
    public ICommand RemoveCommand { get; }
    public ICommand RunCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand AddToleranceRowCommand { get; }
    public ICommand RemoveToleranceRowCommand { get; }
    public ICommand AddFittingRowCommand { get; }
    public ICommand RemoveFittingRowCommand { get; }
    public ICommand CloseCommand { get; }
    public Action? CloseAction { get; set; }

    private void LoadSettings()
    {
        var stored = _settingsRepo.Load();
        var settings = stored ?? BendSettings.Default;
        SettingsNotice = stored is null
            ? "저장된 설정이 없어 기본값(미저장)을 표시합니다. 곡관 치수는 제조사 실측치라 기본값이 없습니다."
            : string.Empty;

        Tolerances.Clear();
        foreach (var e in settings.Tolerance.Entries)
            Tolerances.Add(new BendToleranceRow { PipeKind = e.PipeKind, DiameterMm = e.DiameterMm, ToleranceDeg = e.ToleranceDeg });

        Fittings.Clear();
        foreach (var e in settings.Fittings.Entries)
            Fittings.Add(new BendFittingRow { PipeKind = e.PipeKind, DiameterMm = e.DiameterMm, AngleDeg = e.AngleDeg, Form = e.Form, LayingLengthMm = e.LayingLengthMm, CenterlineRadiusMm = e.CenterlineRadiusMm });
    }

    private BendSettings ToSettings() => new(
        new BendToleranceTable(Tolerances.Select(x => new BendToleranceEntry(x.PipeKind ?? string.Empty, x.DiameterMm, x.ToleranceDeg)).ToList()),
        new BendFittingCatalog(Fittings.Select(x => new BendFittingEntry(x.PipeKind ?? string.Empty, x.DiameterMm, x.AngleDeg, x.Form, x.LayingLengthMm, x.CenterlineRadiusMm)).ToList()));

    private void SaveSettings()
    {
        // 사용자 결정(2026-07-31): t < T 는 저장을 막지 않고 경고만 띄운다.
        var invalid = Fittings.Where(x => x.CenterlineRadiusMm > 0 && x.LayingLengthMm < x.TangentLengthMm).ToList();
        if (invalid.Count > 0)
            _dialog.Warn("곡관 치수 확인", $"t가 접선길이 T보다 작은 행이 {invalid.Count}건 있습니다.\n호가 곡관 몸통 밖으로 나가는 치수이며, 저장과 모델링은 그대로 진행합니다.");

        try { _save.Execute(ToSettings()); _dialog.Info("곡관 설정", "허용굴곡과 곡관 치수를 저장했습니다."); SettingsNotice = string.Empty; }
        catch (Exception ex) { _dialog.Warn("곡관 설정", $"저장에 실패했습니다.\n{ex.Message}"); }
    }

    private void Run()
    {
        if (Files.Count == 0) { _dialog.Warn("입력 확인", "하나 이상의 SHP 또는 DXF 파일을 추가하세요."); return; }
        if (SnapToleranceMm <= 0) { _dialog.Warn("입력 확인", "스냅 허용오차는 0보다 커야 합니다."); return; }

        try
        {
            var result = _analyze.Execute(new PipeNetworkDiagnosisRequest
            {
                Files = Files.Select(x => new AlignmentPlacementFile(x.Path, x.PipeKind)).ToList(),
                SnapToleranceMm = SnapToleranceMm,
                ParseCombinedDiameter = ParseCombinedDiameter,
                Form = Form
            });

            Attention.Clear();
            foreach (var report in result.Attention) Attention.Add(report);

            var kinds = string.Join(", ", result.KindCounts.OrderBy(x => x.Key).Select(x => $"{x.Key} {x.Value}"));
            Summary = $"절점 {result.NodeCount} / 간선 {result.EdgeCount}\n{kinds}\n"
                    + $"곡관 판정 — 표준 {result.BendStandardCount}, 생략 {result.BendNoneCount}, 미해결 {result.BendUnresolvedCount}\n"
                    + $"경고 {result.Warnings.Count}건";

            if (result.SizeConflictCount > 0)
                _dialog.Warn("곡관 치수 확인", $"t가 접선길이 T보다 작은 곡관이 {result.SizeConflictCount}개 있습니다.\n호가 곡관 몸통 밖으로 나가지만 모델링은 진행합니다.");

            if (result.Warnings.Count > 0)
                _dialog.Info("진단 경고", string.Join("\n", result.Warnings.Take(20))
                    + (result.Warnings.Count > 20 ? $"\n… 외 {result.Warnings.Count - 20}건" : string.Empty));
        }
        catch (Exception ex) { _dialog.Warn("관로 네트워크 진단", $"진단에 실패했습니다.\n{ex.Message}"); }
    }

    private void AddFile()
    {
        var path = _fileDialog.OpenFile("관로 선형 파일 선택", "관로 선형 파일 (*.shp;*.dxf)|*.shp;*.dxf|모든 파일 (*.*)|*.*");
        if (string.IsNullOrWhiteSpace(path) || Files.Any(x => string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase))) return;
        var item = new AlignmentPlacementFileItem { Path = path };
        Files.Add(item);
        SelectedFile = item;
    }

    private void RemoveFile()
    {
        if (SelectedFile is null) return;
        Files.Remove(SelectedFile);
        SelectedFile = Files.FirstOrDefault();
    }
}
