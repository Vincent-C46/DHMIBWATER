using System.Collections.ObjectModel;
using System.Windows.Input;
using DHBIMWATER.Core.Gis;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Application.UseCases.Gis;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;

namespace DHBIMWATER.UI.ViewModels.Modeling;

/// <summary>
/// 관·곡관 설정 창의 통합 ViewModel — 직관 제원 · 곡관치수 · 허용굴곡 3탭을 한 창에서 편집한다
/// (2026-08-20, 이전에는 곡관치수까지가 이 창이고 허용굴곡은 별도 창이었다).
/// </summary>
public sealed class PipeSpecTableViewModel : ViewModelBase
{
    private BendSettings _baseSettings;
    private readonly SaveBendSettingsUseCase _save;
    private readonly IDialogService _dialog;
    private readonly IFileDialogService _fileDialog;
    private readonly IBendSettingsFileStore _fileStore;
    private int _selectedFittingTabIndex;
    public PipeSpecTableViewModel(BendSettings source, SaveBendSettingsUseCase save, IDialogService dialog,
        IFileDialogService fileDialog, IBendSettingsFileStore fileStore)
    {
        _baseSettings = source; _save = save; _dialog = dialog; _fileDialog = fileDialog; _fileStore = fileStore;
        Joint = new JointDeflectionSettingsViewModel(source);
        AddStraightCommand = new RelayCommand(_ => StraightRows.Add(new StraightPipeSpecRow()));
        RemoveStraightCommand = new RelayCommand(_ => { if (SelectedStraight is not null) StraightRows.Remove(SelectedStraight); });
        AddFittingCommand = new RelayCommand(_ => ActiveFittingRows().Add(NewFittingRow(ActiveConnection())));
        RemoveFittingCommand = new RelayCommand(_ => RemoveSelectedFitting());
        RestoreCommand = new RelayCommand(_ => LoadDefaults());
        SaveCommand = new RelayCommand(_ => Confirm());
        ExportCommand = new RelayCommand(_ => Export());
        ImportCommand = new RelayCommand(_ => Import());
        CancelCommand = new RelayCommand(_ => CloseAction?.Invoke());
        Load(source.StraightPipes, source.Fittings);
    }
    public ObservableCollection<StraightPipeSpecRow> StraightRows { get; } = new();
    public ObservableCollection<BendFittingRow> SocketFittingRows { get; } = new();
    public ObservableCollection<BendFittingRow> FlangedFittingRows { get; } = new();
    /// <summary>허용굴곡 탭. 독립 창이던 시절의 ViewModel을 그대로 세 번째 탭으로 얹는다.</summary>
    public JointDeflectionSettingsViewModel Joint { get; }
    public StraightPipeSpecRow? SelectedStraight { get; set; }
    public BendFittingRow? SelectedSocketFitting { get; set; }
    public BendFittingRow? SelectedFlangedFitting { get; set; }
    public int SelectedFittingTabIndex { get => _selectedFittingTabIndex; set => SetProperty(ref _selectedFittingTabIndex, value); }
    public ICommand AddStraightCommand { get; } public ICommand RemoveStraightCommand { get; }
    public ICommand AddFittingCommand { get; } public ICommand RemoveFittingCommand { get; }
    public ICommand RestoreCommand { get; } public ICommand SaveCommand { get; } public ICommand ExportCommand { get; } public ICommand ImportCommand { get; } public ICommand CancelCommand { get; }
    public Action? CloseAction { get; set; }
    public BendSettings? Result { get; private set; }

    private static BendFittingRow NewFittingRow(BendConnection connection) => new() { Connection = connection };
    private BendConnection ActiveConnection() => SelectedFittingTabIndex == 1 ? BendConnection.Flanged : BendConnection.Socket;
    private ObservableCollection<BendFittingRow> ActiveFittingRows() => SelectedFittingTabIndex == 1 ? FlangedFittingRows : SocketFittingRows;
    private void RemoveSelectedFitting()
    {
        if (SelectedFittingTabIndex == 1)
        {
            if (SelectedFlangedFitting is not null) FlangedFittingRows.Remove(SelectedFlangedFitting);
        }
        else if (SelectedSocketFitting is not null) SocketFittingRows.Remove(SelectedSocketFitting);
    }
    private void LoadDefaults() { Load(StraightPipeSpecTable.Default, BendFittingCatalog.Default); Joint.RestoreCommand.Execute(null); }
    private void Load(StraightPipeSpecTable straight, BendFittingCatalog fittings)
    {
        StraightRows.Clear(); SocketFittingRows.Clear(); FlangedFittingRows.Clear();
        foreach (var group in straight.Entries.GroupBy(x => x.DiameterMm).OrderBy(x => x.Key))
        {
            var row = new StraightPipeSpecRow { DiameterMm = group.Key, OuterDiameterMm = group.First().OuterDiameterMm };
            foreach (var x in group) SetThickness(row, x.PipeKind, x.ThicknessMm);
            StraightRows.Add(row);
        }
        // DN×각도 한 행 — A형/B형은 더 이상 별도 행이 아니다(2026-08-20, e·R·t는 형식 무관·s와 무게만 다름).
        foreach (var x in fittings.Entries)
        {
            var row = NewFittingRow(x.Connection); row.DiameterMm = x.DiameterMm; row.AngleDeg = x.AngleDeg; row.Form = x.Form;
            row.WallThicknessMm = x.WallThicknessMm; row.LayingLengthMm = x.LayingLengthMm; row.CenterlineRadiusMm = x.CenterlineRadiusMm;
            row.WeightKpMechanicalKg = x.WeightKpMechanicalKg; row.WeightTytonKg = x.WeightTytonKg;
            (x.Connection == BendConnection.Flanged ? FlangedFittingRows : SocketFittingRows).Add(row);
        }
    }
    private void Confirm()
    {
        Result = BuildResult();
        WarnOuterDiameterConflicts(Result);
        try { _save.Execute(Result); }
        catch (Exception ex) { _dialog.Warn("관로 규격 설정", $"저장에 실패했습니다.\n{ex.Message}"); }
        CloseAction?.Invoke();
    }
    private void Export()
    {
        var path = _fileDialog.SaveFile("관·곡관 설정 내보내기", "JSON 파일 (*.json)|*.json", "DHBIMWATER_PipeSettings.json");
        if (string.IsNullOrWhiteSpace(path)) return;
        var settings = BuildResult();
        WarnOuterDiameterConflicts(settings);
        try
        {
            _fileStore.Save(path, settings);
            _dialog.Info("관로 규격 설정", $"설정 파일을 내보냈습니다.\n{path}");
        }
        catch (Exception ex) { _dialog.Warn("관로 규격 설정", $"설정 파일을 내보내지 못했습니다.\n{ex.Message}"); }
    }
    private void Import()
    {
        var path = _fileDialog.OpenFile("관·곡관 설정 불러오기", "JSON 파일 (*.json)|*.json");
        if (string.IsNullOrWhiteSpace(path)) return;
        try
        {
            var settings = _fileStore.Load(path);
            _baseSettings = settings;
            Load(settings.StraightPipes, settings.Fittings);
            Joint.LoadSettings(settings);
            Result = null;
            _dialog.Info("관로 규격 설정", "설정 파일을 불러왔습니다. 내용을 확인한 뒤 [확인]을 눌러 현재 프로젝트에 저장하세요.");
        }
        catch (Exception ex) { _dialog.Warn("관로 규격 설정", $"설정 파일을 불러오지 못했습니다.\n{ex.Message}"); }
    }
    private BendSettings BuildResult()
    {
        var straight = new StraightPipeSpecTable(StraightRows.Where(x => x.DiameterMm > 0 && x.OuterDiameterMm > 0).SelectMany(x => x.ToSpecs()).Where(x => x.ThicknessMm > 0).ToList());
        var fittings = new BendFittingCatalog(SocketFittingRows.Concat(FlangedFittingRows)
            .Where(x => x.DiameterMm > 0 && x.AngleDeg > 0 && x.LayingLengthMm > 0 && x.CenterlineRadiusMm > 0)
            .Select(x => new BendFittingEntry(x.DiameterMm, x.AngleDeg, x.Form, x.LayingLengthMm, x.CenterlineRadiusMm, x.WallThicknessMm, x.WeightKpMechanicalKg, x.WeightTytonKg, Connection: x.Connection))
            .ToList());
        return Joint.BuildResult(_baseSettings with { StraightPipes = straight, Fittings = fittings });
    }
    private void WarnOuterDiameterConflicts(BendSettings settings)
    {
        var conflicts = settings.StraightPipes.FindOuterDiameterConflicts();
        if (conflicts.Count > 0)
            _dialog.Warn("직관 제원 확인", $"같은 DN의 OD가 관종별로 다른 항목: {string.Join(", ", conflicts.Select(x => $"DN{x:0.##}"))}");
    }
    private static void SetThickness(StraightPipeSpecRow r, string kind, double value)
    {
        switch (kind) { case PipeKindCatalog.Water1: r.Water1 = value; break; case PipeKindCatalog.Water2: r.Water2 = value; break; case PipeKindCatalog.Water3: r.Water3 = value; break; case PipeKindCatalog.Water4: r.Water4 = value; break; case PipeKindCatalog.Sewer1: r.Sewer1 = value; break; case PipeKindCatalog.Sewer2: r.Sewer2 = value; break; case PipeKindCatalog.Sewer3: r.Sewer3 = value; break; }
    }
}
