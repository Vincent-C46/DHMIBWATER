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
    private double _diameterMm, _angleDeg = 45d, _layingLengthMm, _centerlineRadiusMm, _extraLegLengthMm, _wallThicknessMm;
    private string _familyTypeName = string.Empty, _adaptivePointStatus = "미확인";
    private BendForm _form = BendForm.AType;
    public string PipeKind { get => _pipeKind; set => SetProperty(ref _pipeKind, value); }
    public double DiameterMm { get => _diameterMm; set => SetProperty(ref _diameterMm, value); }
    public double AngleDeg { get => _angleDeg; set { if (SetProperty(ref _angleDeg, value)) OnPropertyChanged(nameof(TangentLengthMm)); } }
    public BendForm Form { get => _form; set => SetProperty(ref _form, value); }
    public double LayingLengthMm { get => _layingLengthMm; set { if (SetProperty(ref _layingLengthMm, value)) OnPropertyChanged(nameof(LongLegLengthMm)); } }
    public double CenterlineRadiusMm { get => _centerlineRadiusMm; set { if (SetProperty(ref _centerlineRadiusMm, value)) OnPropertyChanged(nameof(TangentLengthMm)); } }
    /// <summary>s — B형에서 한쪽에만 더 붙는 직관부(mm). A형은 0. 긴 쪽은 폴리선 진행 방향 기준 하류쪽에 붙는다.</summary>
    public double ExtraLegLengthMm { get => _extraLegLengthMm; set { if (SetProperty(ref _extraLegLengthMm, value)) OnPropertyChanged(nameof(LongLegLengthMm)); } }
    /// <summary>e — 곡관 패밀리의 thk 인스턴스 매개변수에 대응하는 벽 두께(mm).</summary>
    public double WallThicknessMm { get => _wallThicknessMm; set => SetProperty(ref _wallThicknessMm, value); }
    /// <summary>콤보박스 표시·선택용 "패밀리명 : 타입명" 값.</summary>
    public string FamilyTypeName { get => _familyTypeName; set { if (SetProperty(ref _familyTypeName, value)) RefreshAdaptivePointStatus(); } }
    public string FamilyName => SplitFamilyTypeName().FamilyName;
    public string TypeName => SplitFamilyTypeName().TypeName;
    public string AdaptivePointStatus { get => _adaptivePointStatus; private set { if (SetProperty(ref _adaptivePointStatus, value)) OnPropertyChanged(nameof(HasAdaptivePointWarning)); } }
    /// <summary>3점 등 명확히 5점이 아닌 경우만 경고한다. 확인 불가는 배치 시점 검증으로 넘긴다.</summary>
    public bool HasAdaptivePointWarning => !string.IsNullOrWhiteSpace(FamilyTypeName) && AdaptivePointStatus != "5점 확인" && AdaptivePointStatus != "미확인";
    public Func<string, int>? AdaptiveBendPointCountProvider { get; init; }
    /// <summary>긴 쪽 관 끝까지의 거리 t + s(mm). 확인용 읽기 전용 열이다.</summary>
    public double LongLegLengthMm => Math.Round(LayingLengthMm + ExtraLegLengthMm, 1);
    public double TangentLengthMm => Math.Round(BendResolver.TangentLength(CenterlineRadiusMm, AngleDeg), 1);
    private (string FamilyName, string TypeName) SplitFamilyTypeName() { var separator = FamilyTypeName.LastIndexOf(" : ", StringComparison.Ordinal); return separator <= 0 || separator >= FamilyTypeName.Length - 3 ? (string.Empty, string.Empty) : (FamilyTypeName[..separator], FamilyTypeName[(separator + 3)..]); }
    private void RefreshAdaptivePointStatus() { var count = string.IsNullOrWhiteSpace(FamilyTypeName) ? -1 : AdaptiveBendPointCountProvider?.Invoke(FamilyTypeName) ?? -1; AdaptivePointStatus = count < 0 ? "미확인" : count == 5 ? "5점 확인" : $"⚠ {count}점 (5점 필요)"; OnPropertyChanged(nameof(FamilyName)); OnPropertyChanged(nameof(TypeName)); }
}
