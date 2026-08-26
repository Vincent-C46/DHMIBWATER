using DHBIMWATER.UI.Base;

namespace DHBIMWATER.UI.ViewModels.Modeling;

public sealed record DiameterMappingOption(string Display, double? DiameterMm);

public sealed class DiameterMappingRow : ViewModelBase
{
    private double? _diameterMm;
    private string _pipeKind = string.Empty;
    private IReadOnlyList<DiameterMappingOption> _diameterOptions = Array.Empty<DiameterMappingOption>();
    private DiameterMappingOption? _selectedDiameterOption;
    private double? _outerDiameterMm, _thicknessMm;
    private string _status = string.Empty;
    private bool _isSelected;

    public required string RawValue { get; init; }
    public required string FileName { get; init; }
    public required string FileKey { get; init; }
    public required int Count { get; init; }
    public double? AutoDiameterMm { get; init; }
    public string AutoDiameterDisplay => AutoDiameterMm is > 0 ? $"{AutoDiameterMm:0.##}" : "(실패)";
    public double? DiameterMm
    {
        get => _diameterMm;
        set
        {
            if (!SetProperty(ref _diameterMm, value)) return;
            IsUserModified = true;
            SyncSelectedDiameterOption();
            Changed?.Invoke();
        }
    }
    public DiameterMappingOption? SelectedDiameterOption
    {
        get => _selectedDiameterOption;
        set
        {
            if (!SetProperty(ref _selectedDiameterOption, value)) return;
            DiameterMm = value?.DiameterMm;
        }
    }
    public string PipeKind
    {
        get => _pipeKind;
        set
        {
            if (!SetProperty(ref _pipeKind, value)) return;
            IsUserModified = true;
            Changed?.Invoke();
        }
    }
    public bool IsUserModified { get; private set; }
    public double? OuterDiameterMm { get => _outerDiameterMm; private set => SetProperty(ref _outerDiameterMm, value); }
    public double? ThicknessMm { get => _thicknessMm; private set => SetProperty(ref _thicknessMm, value); }
    public string SpecificationDisplay => OuterDiameterMm is > 0 && ThicknessMm is > 0 ? $"{OuterDiameterMm:0.##}/{ThicknessMm:0.##}" : "-";
    public string Status { get => _status; private set => SetProperty(ref _status, value); }
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
    public Action? Changed { get; init; }

    public void InitializeValues(IReadOnlyList<DiameterMappingOption> diameterOptions, double? diameterMm, string pipeKind, bool isUserModified)
    {
        _diameterOptions = diameterOptions;
        _diameterMm = diameterMm;
        _pipeKind = pipeKind;
        _selectedDiameterOption = FindDiameterOption(diameterMm);
        IsUserModified = isUserModified;
    }

    public void SetPreview(double? outerDiameterMm, double? thicknessMm, string status)
    {
        OuterDiameterMm = outerDiameterMm;
        ThicknessMm = thicknessMm;
        OnPropertyChanged(nameof(SpecificationDisplay));
        Status = status;
    }

    private void SyncSelectedDiameterOption()
    {
        var option = FindDiameterOption(DiameterMm);
        SetProperty(ref _selectedDiameterOption, option, nameof(SelectedDiameterOption));
    }

    private DiameterMappingOption? FindDiameterOption(double? diameterMm)
        => _diameterOptions.FirstOrDefault(x => Nullable.Equals(x.DiameterMm, diameterMm));
}
