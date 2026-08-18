using DHBIMWATER.Core.Gis;
using DHBIMWATER.UI.Base;

namespace DHBIMWATER.UI.ViewModels.Modeling;

public sealed class StraightPipeSpecRow : ViewModelBase
{
    private double _diameterMm, _outerDiameterMm, _water1, _water2, _water3, _water4, _sewer1, _sewer2, _sewer3;
    public double DiameterMm { get => _diameterMm; set => SetProperty(ref _diameterMm, value); }
    public double OuterDiameterMm { get => _outerDiameterMm; set => SetProperty(ref _outerDiameterMm, value); }
    public double Water1 { get => _water1; set => SetProperty(ref _water1, value); }
    public double Water2 { get => _water2; set => SetProperty(ref _water2, value); }
    public double Water3 { get => _water3; set => SetProperty(ref _water3, value); }
    public double Water4 { get => _water4; set => SetProperty(ref _water4, value); }
    public double Sewer1 { get => _sewer1; set => SetProperty(ref _sewer1, value); }
    public double Sewer2 { get => _sewer2; set => SetProperty(ref _sewer2, value); }
    public double Sewer3 { get => _sewer3; set => SetProperty(ref _sewer3, value); }

    public IEnumerable<StraightPipeSpec> ToSpecs()
    {
        yield return new(PipeKindCatalog.Water1, DiameterMm, OuterDiameterMm, Water1);
        yield return new(PipeKindCatalog.Water2, DiameterMm, OuterDiameterMm, Water2);
        yield return new(PipeKindCatalog.Water3, DiameterMm, OuterDiameterMm, Water3);
        yield return new(PipeKindCatalog.Water4, DiameterMm, OuterDiameterMm, Water4);
        yield return new(PipeKindCatalog.Sewer1, DiameterMm, OuterDiameterMm, Sewer1);
        yield return new(PipeKindCatalog.Sewer2, DiameterMm, OuterDiameterMm, Sewer2);
        yield return new(PipeKindCatalog.Sewer3, DiameterMm, OuterDiameterMm, Sewer3);
    }
}

public sealed class JointDeflectionRow : ViewModelBase
{
    private string _jointType = JointTypeCatalog.KpMechanical;
    private double _diameterMm, _allowableDeg;
    public string JointType { get => _jointType; set => SetProperty(ref _jointType, value); }
    public double DiameterMm { get => _diameterMm; set => SetProperty(ref _diameterMm, value); }
    public double AllowableDeg { get => _allowableDeg; set => SetProperty(ref _allowableDeg, value); }
}

public sealed class BendFittingRow : ViewModelBase
{
    private double _diameterMm, _angleDeg = 45d, _layingLengthMm, _centerlineRadiusMm, _extraLegLengthMm, _wallThicknessMm;
    private string _typeName = string.Empty, _adaptivePointStatus = "미확인";
    private BendForm _form = BendForm.AType;
    public double DiameterMm { get => _diameterMm; set => SetProperty(ref _diameterMm, value); }
    public double AngleDeg { get => _angleDeg; set { if (SetProperty(ref _angleDeg, value)) OnPropertyChanged(nameof(TangentLengthMm)); } }
    public BendForm Form { get => _form; set => SetProperty(ref _form, value); }
    public double LayingLengthMm { get => _layingLengthMm; set { if (SetProperty(ref _layingLengthMm, value)) OnPropertyChanged(nameof(LongLegLengthMm)); } }
    public double CenterlineRadiusMm { get => _centerlineRadiusMm; set { if (SetProperty(ref _centerlineRadiusMm, value)) OnPropertyChanged(nameof(TangentLengthMm)); } }
    public double ExtraLegLengthMm { get => _extraLegLengthMm; set { if (SetProperty(ref _extraLegLengthMm, value)) OnPropertyChanged(nameof(LongLegLengthMm)); } }
    public double WallThicknessMm { get => _wallThicknessMm; set => SetProperty(ref _wallThicknessMm, value); }
    public string TypeName { get => _typeName; set { if (SetProperty(ref _typeName, value)) RefreshAdaptivePointStatus(); } }
    public string AdaptivePointStatus { get => _adaptivePointStatus; private set { if (SetProperty(ref _adaptivePointStatus, value)) OnPropertyChanged(nameof(HasAdaptivePointWarning)); } }
    public bool HasAdaptivePointWarning => !string.IsNullOrWhiteSpace(TypeName) && AdaptivePointStatus != "5점 확인" && AdaptivePointStatus != "미확인";
    public Func<string, int>? AdaptivePointCountProvider { get; init; }
    public double LongLegLengthMm => Math.Round(LayingLengthMm + ExtraLegLengthMm, 1);
    public double TangentLengthMm => Math.Round(BendResolver.TangentLength(CenterlineRadiusMm, AngleDeg), 1);
    private void RefreshAdaptivePointStatus() { var count = string.IsNullOrWhiteSpace(TypeName) ? -1 : AdaptivePointCountProvider?.Invoke(TypeName) ?? -1; AdaptivePointStatus = count < 0 ? "미확인" : count == 5 ? "5점 확인" : $"⚠ {count}점 (5점 필요)"; }
}
