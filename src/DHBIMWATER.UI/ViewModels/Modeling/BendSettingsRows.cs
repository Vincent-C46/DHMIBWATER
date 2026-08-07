using DHBIMWATER.Core.Gis;
using DHBIMWATER.UI.Base;

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
    private double _diameterMm, _angleDeg = 45d, _layingLengthMm, _centerlineRadiusMm, _extraLegLengthMm;
    private BendForm _form = BendForm.AType;
    public string PipeKind { get => _pipeKind; set => SetProperty(ref _pipeKind, value); }
    public double DiameterMm { get => _diameterMm; set => SetProperty(ref _diameterMm, value); }
    public double AngleDeg { get => _angleDeg; set { if (SetProperty(ref _angleDeg, value)) OnPropertyChanged(nameof(TangentLengthMm)); } }
    public BendForm Form { get => _form; set => SetProperty(ref _form, value); }
    public double LayingLengthMm { get => _layingLengthMm; set { if (SetProperty(ref _layingLengthMm, value)) OnPropertyChanged(nameof(LongLegLengthMm)); } }
    public double CenterlineRadiusMm { get => _centerlineRadiusMm; set { if (SetProperty(ref _centerlineRadiusMm, value)) OnPropertyChanged(nameof(TangentLengthMm)); } }
    /// <summary>s — B형에서 한쪽에만 더 붙는 직관부(mm). A형은 0. 긴 쪽은 폴리선 진행 방향 기준 하류쪽에 붙는다.</summary>
    public double ExtraLegLengthMm { get => _extraLegLengthMm; set { if (SetProperty(ref _extraLegLengthMm, value)) OnPropertyChanged(nameof(LongLegLengthMm)); } }
    /// <summary>긴 쪽 관 끝까지의 거리 t + s(mm). 확인용 읽기 전용 열이다.</summary>
    public double LongLegLengthMm => Math.Round(LayingLengthMm + ExtraLegLengthMm, 1);
    public double TangentLengthMm => Math.Round(BendResolver.TangentLength(CenterlineRadiusMm, AngleDeg), 1);
}
