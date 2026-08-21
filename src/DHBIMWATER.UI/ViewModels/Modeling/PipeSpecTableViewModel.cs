using System.Collections.ObjectModel;
using System.Windows.Input;
using DHBIMWATER.Core.Gis;
using DHBIMWATER.Application.Interfaces;
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
    private readonly BendSettings _source;
    private readonly SaveBendSettingsUseCase _save;
    private readonly IDialogService _dialog;
    public PipeSpecTableViewModel(BendSettings source, SaveBendSettingsUseCase save, IDialogService dialog)
    {
        _source = source; _save = save; _dialog = dialog;
        Joint = new JointDeflectionSettingsViewModel(source);
        AddStraightCommand = new RelayCommand(_ => StraightRows.Add(new StraightPipeSpecRow()));
        RemoveStraightCommand = new RelayCommand(_ => { if (SelectedStraight is not null) StraightRows.Remove(SelectedStraight); });
        AddFittingCommand = new RelayCommand(_ => FittingRows.Add(NewFittingRow()));
        RemoveFittingCommand = new RelayCommand(_ => { if (SelectedFitting is not null) FittingRows.Remove(SelectedFitting); });
        RestoreCommand = new RelayCommand(_ => LoadDefaults());
        SaveCommand = new RelayCommand(_ => Confirm());
        SaveToMasterCommand = new RelayCommand(_ => SaveToMaster());
        CancelCommand = new RelayCommand(_ => CloseAction?.Invoke());
        Load(source.StraightPipes, source.Fittings);
    }
    public ObservableCollection<StraightPipeSpecRow> StraightRows { get; } = new();
    public ObservableCollection<BendFittingRow> FittingRows { get; } = new();
    /// <summary>허용굴곡 탭. 독립 창이던 시절의 ViewModel을 그대로 세 번째 탭으로 얹는다.</summary>
    public JointDeflectionSettingsViewModel Joint { get; }
    public StraightPipeSpecRow? SelectedStraight { get; set; }
    public BendFittingRow? SelectedFitting { get; set; }
    public ICommand AddStraightCommand { get; } public ICommand RemoveStraightCommand { get; }
    public ICommand AddFittingCommand { get; } public ICommand RemoveFittingCommand { get; }
    public ICommand RestoreCommand { get; } public ICommand SaveCommand { get; } public ICommand SaveToMasterCommand { get; } public ICommand CancelCommand { get; }
    public Action? CloseAction { get; set; }
    public BendSettings? Result { get; private set; }

    private static BendFittingRow NewFittingRow() => new();
    private void LoadDefaults() { Load(StraightPipeSpecTable.Default, BendFittingCatalog.Default); Joint.RestoreCommand.Execute(null); }
    private void Load(StraightPipeSpecTable straight, BendFittingCatalog fittings)
    {
        StraightRows.Clear(); FittingRows.Clear();
        foreach (var group in straight.Entries.GroupBy(x => x.DiameterMm).OrderBy(x => x.Key))
        {
            var row = new StraightPipeSpecRow { DiameterMm = group.Key, OuterDiameterMm = group.First().OuterDiameterMm };
            foreach (var x in group) SetThickness(row, x.PipeKind, x.ThicknessMm);
            StraightRows.Add(row);
        }
        // DN×각도 한 행 — A형/B형은 더 이상 별도 행이 아니다(2026-08-20, e·R·t는 형식 무관·s와 무게만 다름).
        foreach (var x in fittings.Entries)
        {
            var row = NewFittingRow(); row.DiameterMm = x.DiameterMm; row.AngleDeg = x.AngleDeg; row.Form = x.Form;
            row.WallThicknessMm = x.WallThicknessMm; row.LayingLengthMm = x.LayingLengthMm; row.CenterlineRadiusMm = x.CenterlineRadiusMm;
            row.WeightKpMechanicalKg = x.WeightKpMechanicalKg; row.WeightTytonKg = x.WeightTytonKg;
            FittingRows.Add(row);
        }
    }
    private void Confirm()
    {
        Result = BuildResult();
        WarnOuterDiameterConflicts(Result);
        try { _save.Execute(Result, BendSettingsScope.Project); }
        catch (Exception ex) { _dialog.Warn("관로 규격 설정", $"저장에 실패했습니다.\n{ex.Message}"); }
        CloseAction?.Invoke();
    }
    private void SaveToMaster()
    {
        var settings = BuildResult();
        WarnOuterDiameterConflicts(settings);
        try
        {
            _save.Execute(settings, BendSettingsScope.Master);
            _dialog.Info("관로 규격 설정", "직관·곡관 규격과 허용굴곡 설정을 이 프로젝트와 마스터에 저장했습니다.");
        }
        catch (Exception ex) { _dialog.Warn("관로 규격 설정", $"저장에 실패했습니다.\n{ex.Message}"); }
    }
    private BendSettings BuildResult()
    {
        var straight = new StraightPipeSpecTable(StraightRows.Where(x => x.DiameterMm > 0 && x.OuterDiameterMm > 0).SelectMany(x => x.ToSpecs()).Where(x => x.ThicknessMm > 0).ToList());
        var fittings = new BendFittingCatalog(FittingRows.Where(x => x.DiameterMm > 0 && x.AngleDeg > 0 && x.LayingLengthMm > 0 && x.CenterlineRadiusMm > 0)
            .Select(x => new BendFittingEntry(x.DiameterMm, x.AngleDeg, x.Form, x.LayingLengthMm, x.CenterlineRadiusMm, x.WallThicknessMm, x.WeightKpMechanicalKg, x.WeightTytonKg))
            .ToList());
        return Joint.BuildResult(_source with { StraightPipes = straight, Fittings = fittings });
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
