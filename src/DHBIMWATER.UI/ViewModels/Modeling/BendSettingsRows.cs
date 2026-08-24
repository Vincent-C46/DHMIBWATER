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
    private double _diameterMm, _angleDeg = 45d, _layingLengthMm, _centerlineRadiusMm, _wallThicknessMm, _weightKpMechanicalKg, _weightTytonKg;
    private BendForm _form = BendForm.BType;
    private BendConnection _connection = BendConnection.Socket;
    public double DiameterMm { get => _diameterMm; set => SetProperty(ref _diameterMm, value); }
    public double AngleDeg { get => _angleDeg; set { if (SetProperty(ref _angleDeg, value)) OnPropertyChanged(nameof(TangentLengthMm)); } }
    /// <summary>기본은 B형(소켓+스피것). A형이 필요한 자리만 사용자가 바꾸며, 바뀌면 그 형식의 핸드북 무게로 다시 채운다.</summary>
    public BendForm Form { get => _form; set { if (SetProperty(ref _form, value)) { OnPropertyChanged(nameof(LongLegLengthMm)); OnPropertyChanged(nameof(ExtraLegLengthMm)); RefreshWeightDefaults(); } } }
    /// <summary>그리드에는 노출하지 않고 소켓/플랜지 컬렉션을 저장할 때 접합종류를 보존한다.</summary>
    public BendConnection Connection { get => _connection; set => SetProperty(ref _connection, value); }
    public double LayingLengthMm { get => _layingLengthMm; set { if (SetProperty(ref _layingLengthMm, value)) OnPropertyChanged(nameof(LongLegLengthMm)); } }
    public double CenterlineRadiusMm { get => _centerlineRadiusMm; set { if (SetProperty(ref _centerlineRadiusMm, value)) OnPropertyChanged(nameof(TangentLengthMm)); } }
    /// <summary>s — B형만 200mm, A형은 0. 핸드북 고정값이라 계산만 하고 편집 대상이 아니다.</summary>
    public double ExtraLegLengthMm => Form == BendForm.BType ? 200d : 0d;
    public double WallThicknessMm { get => _wallThicknessMm; set => SetProperty(ref _wallThicknessMm, value); }
    /// <summary>KP 메커니컬 조인트 기준 무게(kg). 현재 <see cref="Form"/>에 대응하는 값이다.</summary>
    public double WeightKpMechanicalKg { get => _weightKpMechanicalKg; set => SetProperty(ref _weightKpMechanicalKg, value); }
    /// <summary>타이튼 조인트 기준 무게(kg). 현재 <see cref="Form"/>에 대응하는 값이다.</summary>
    public double WeightTytonKg { get => _weightTytonKg; set => SetProperty(ref _weightTytonKg, value); }
    // 열 제거(2026-08-21) — 배치 계산은 BendResolution.LongLegLengthMm을 쓴다.
    public double LongLegLengthMm => Math.Round(LayingLengthMm + ExtraLegLengthMm, 1);
    public double TangentLengthMm => Math.Round(BendResolver.TangentLength(CenterlineRadiusMm, AngleDeg), 1);
    /// <summary>형식(A/B) 전환 시 그 형식의 핸드북 무게로 다시 채운다. 이후 사용자가 손으로 고치면 그 값을 유지한다.</summary>
    // 형식 열 제거(2026-08-21)로 현재 호출부 없음 — 무게 재계산이 필요해지면 여기서 이어간다.
    public void RefreshWeightDefaults()
    {
        if (Connection == BendConnection.Socket && BendFittingCatalog.TryGetHandbookWeight(DiameterMm, AngleDeg, Form, out var kp, out var tyton))
        {
            WeightKpMechanicalKg = kp; WeightTytonKg = tyton;
        }
    }
}
