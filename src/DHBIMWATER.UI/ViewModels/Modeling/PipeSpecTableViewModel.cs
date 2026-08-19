using System.Collections.ObjectModel;
using System.Windows.Input;
using DHBIMWATER.Core.Gis;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;

namespace DHBIMWATER.UI.ViewModels.Modeling;

/// <summary>
/// 곡관 유형 콤보박스 항목. 화면에는 "패밀리 : 유형"으로 보이지만 카탈로그에 저장되는 값은 유형명뿐이다
/// (패밀리는 모델링 창의 [5점 가변 곡관 패밀리]에서 한 번만 지정한다).
/// </summary>
public sealed record BendTypeOption(string Display, string TypeName);

public sealed class PipeSpecTableViewModel : ViewModelBase
{
    private readonly BendSettings _source;
    private readonly Func<string, int> _pointCount;
    public PipeSpecTableViewModel(BendSettings source, IReadOnlyList<BendTypeOption> bendTypeOptions, Func<string, int> pointCount)
    {
        _source = source; BendTypeOptions = bendTypeOptions; _pointCount = pointCount;
        AddStraightCommand = new RelayCommand(_ => StraightRows.Add(new StraightPipeSpecRow()));
        RemoveStraightCommand = new RelayCommand(_ => { if (SelectedStraight is not null) StraightRows.Remove(SelectedStraight); });
        AddFittingCommand = new RelayCommand(_ => FittingRows.Add(NewFittingRow()));
        RemoveFittingCommand = new RelayCommand(_ => { if (SelectedFitting is not null) FittingRows.Remove(SelectedFitting); });
        RestoreCommand = new RelayCommand(_ => LoadDefaults());
        SaveCommand = new RelayCommand(_ => Confirm()); CancelCommand = new RelayCommand(_ => CloseAction?.Invoke());
        Load(source.StraightPipes, source.Fittings);
    }
    public ObservableCollection<StraightPipeSpecRow> StraightRows { get; } = new();
    public ObservableCollection<BendFittingRow> FittingRows { get; } = new();
    public IReadOnlyList<BendTypeOption> BendTypeOptions { get; }
    public IReadOnlyList<BendForm> Forms { get; } = Enum.GetValues<BendForm>();
    public StraightPipeSpecRow? SelectedStraight { get; set; }
    public BendFittingRow? SelectedFitting { get; set; }
    public ICommand AddStraightCommand { get; } public ICommand RemoveStraightCommand { get; }
    public ICommand AddFittingCommand { get; } public ICommand RemoveFittingCommand { get; }
    public ICommand RestoreCommand { get; } public ICommand SaveCommand { get; } public ICommand CancelCommand { get; }
    public Action? CloseAction { get; set; }
    public BendSettings? Result { get; private set; }

    private BendFittingRow NewFittingRow() => new() { AdaptivePointCountProvider = _pointCount };
    private void LoadDefaults() => Load(StraightPipeSpecTable.Default, BendFittingCatalog.Default);
    private void Load(StraightPipeSpecTable straight, BendFittingCatalog fittings)
    {
        StraightRows.Clear(); FittingRows.Clear();
        foreach (var group in straight.Entries.GroupBy(x => x.DiameterMm).OrderBy(x => x.Key))
        {
            var row = new StraightPipeSpecRow { DiameterMm = group.Key, OuterDiameterMm = group.First().OuterDiameterMm };
            foreach (var x in group) SetThickness(row, x.PipeKind, x.ThicknessMm);
            StraightRows.Add(row);
        }
        foreach (var x in fittings.Entries)
        {
            var row = NewFittingRow(); row.DiameterMm = x.DiameterMm; row.AngleDeg = x.AngleDeg; row.Form = x.Form;
            row.WallThicknessMm = x.WallThicknessMm; row.LayingLengthMm = x.LayingLengthMm; row.CenterlineRadiusMm = x.CenterlineRadiusMm;
            row.ExtraLegLengthMm = x.ExtraLegLengthMm; row.TypeName = x.TypeName ?? string.Empty; FittingRows.Add(row);
        }
    }
    private void Confirm()
    {
        var straight = new StraightPipeSpecTable(StraightRows.Where(x => x.DiameterMm > 0 && x.OuterDiameterMm > 0).SelectMany(x => x.ToSpecs()).Where(x => x.ThicknessMm > 0).ToList());
        var fittings = new BendFittingCatalog(FittingRows.Where(x => x.DiameterMm > 0 && x.AngleDeg > 0 && x.LayingLengthMm > 0 && x.CenterlineRadiusMm > 0).Select(x => new BendFittingEntry(x.DiameterMm, x.AngleDeg, x.Form, x.LayingLengthMm, x.CenterlineRadiusMm, x.ExtraLegLengthMm, x.WallThicknessMm, string.IsNullOrWhiteSpace(x.TypeName) ? null : x.TypeName)).ToList());
        Result = _source with { StraightPipes = straight, Fittings = fittings }; CloseAction?.Invoke();
    }
    private static void SetThickness(StraightPipeSpecRow r, string kind, double value)
    {
        switch (kind) { case PipeKindCatalog.Water1: r.Water1 = value; break; case PipeKindCatalog.Water2: r.Water2 = value; break; case PipeKindCatalog.Water3: r.Water3 = value; break; case PipeKindCatalog.Water4: r.Water4 = value; break; case PipeKindCatalog.Sewer1: r.Sewer1 = value; break; case PipeKindCatalog.Sewer2: r.Sewer2 = value; break; case PipeKindCatalog.Sewer3: r.Sewer3 = value; break; }
    }
}
