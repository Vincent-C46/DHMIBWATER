using System.Collections.ObjectModel;
using System.Windows.Input;
using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Core.Gis;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;

namespace DHBIMWATER.UI.ViewModels.Modeling;

public sealed class PipingFileItem : ViewModelBase
{
    public required string Path { get; init; }
    public required ShapefileReadResult ReadResult { get; init; }
    private string _pipeKind = string.Empty;
    public string FileName => System.IO.Path.GetFileName(Path);
    public string PipeKind { get => _pipeKind; set => SetProperty(ref _pipeKind, value); }
    public string Summary => $"레코드 {ReadResult.RecordCount:N0} · 정점 {ReadResult.VertexCount:N0} · 구간 {ReadResult.SegmentCount:N0}";
}

public sealed class PipingViewModel : ViewModelBase
{
    private readonly IFileDialogService _fileDialog;
    private readonly IShapefileReader _reader;
    private readonly IDialogService _dialog;
    private PipingFileItem? _selectedFile;
    private double _referenceX, _referenceY, _referenceZ;
    private ZSource _zSource = ZSource.GeometryZ;
    private ZDatum _zDatum = ZDatum.AsIs;
    private bool _parseCombinedDiameter = true;

    public PipingViewModel(IFileDialogService fileDialog, IShapefileReader reader, IDialogService dialog)
    {
        _fileDialog = fileDialog; _reader = reader; _dialog = dialog;
        AddCommand = new RelayCommand(_ => AddFile()); RemoveCommand = new RelayCommand(_ => RemoveFile());
        CreateCommand = new RelayCommand(_ => RequestImport()); CancelCommand = new RelayCommand(_ => CloseAction?.Invoke());
    }
    public ObservableCollection<PipingFileItem> Files { get; } = new();
    public PipingFileItem? SelectedFile { get => _selectedFile; set { if (SetProperty(ref _selectedFile, value)) OnPropertyChanged(nameof(SelectedFileDetails)); } }
    public ICommand AddCommand { get; } public ICommand RemoveCommand { get; } public ICommand CreateCommand { get; } public ICommand CancelCommand { get; }
    public Action? CloseAction { get; set; }
    public PipeAlignmentImportRequest? RequestedImport { get; private set; }
    public double ReferenceX { get => _referenceX; set => SetProperty(ref _referenceX, value); }
    public double ReferenceY { get => _referenceY; set => SetProperty(ref _referenceY, value); }
    public double ReferenceZ { get => _referenceZ; set => SetProperty(ref _referenceZ, value); }
    public ZSource ZSource { get => _zSource; set => SetProperty(ref _zSource, value); }
    public ZDatum ZDatum { get => _zDatum; set => SetProperty(ref _zDatum, value); }
    public bool ParseCombinedDiameter { get => _parseCombinedDiameter; set => SetProperty(ref _parseCombinedDiameter, value); }
    public Array ZSources => Enum.GetValues(typeof(ZSource)); public Array ZDatums => Enum.GetValues(typeof(ZDatum));
    public string SelectedFileDetails => SelectedFile is null ? "파일을 추가하면 DBF 필드와 샘플 속성이 표시됩니다." :
        $"{SelectedFile.Summary}\n필드: {string.Join(", ", SelectedFile.ReadResult.Fields.Select(x => x.Name))}\n샘플: {string.Join(", ", (SelectedFile.ReadResult.SampleAttributes ?? new Dictionary<string, string>()).Select(x => $"{x.Key}={x.Value}"))}\n인코딩: {SelectedFile.ReadResult.EncodingName}";
    public string ProjectionDetails => SelectedFile is null ? "" : $"추천: {SelectedFile.ReadResult.RecommendedEpsg ?? "없음"}\n{SelectedFile.ReadResult.ProjectionWkt ?? ".prj 파일 없음"}";

    private void AddFile()
    {
        var path = _fileDialog.OpenFile("관로 SHP 파일 선택", "Shapefile (*.shp)|*.shp|모든 파일 (*.*)|*.*");
        if (string.IsNullOrWhiteSpace(path) || Files.Any(x => string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase))) return;
        try
        {
            var item = new PipingFileItem { Path = path, ReadResult = _reader.Read(path) };
            Files.Add(item); SelectedFile = item; RefreshReferencePoint(); OnPropertyChanged(nameof(ProjectionDetails));
        }
        catch (Exception ex) { _dialog.Warn("SHP 읽기 실패", ex.Message); }
    }
    private void RemoveFile()
    {
        if (SelectedFile is null) return;
        Files.Remove(SelectedFile); SelectedFile = Files.FirstOrDefault(); RefreshReferencePoint(); OnPropertyChanged(nameof(ProjectionDetails));
    }
    private void RefreshReferencePoint()
    {
        if (Files.Count == 0) { ReferenceX = ReferenceY = ReferenceZ = 0; return; }
        ReferenceX = Math.Round((Files.Min(x => x.ReadResult.Extent.XMin) + Files.Max(x => x.ReadResult.Extent.XMax)) / 2, MidpointRounding.AwayFromZero);
        ReferenceY = Math.Round((Files.Min(x => x.ReadResult.Extent.YMin) + Files.Max(x => x.ReadResult.Extent.YMax)) / 2, MidpointRounding.AwayFromZero);
        ReferenceZ = Math.Round((Files.Min(x => x.ReadResult.Extent.ZMin) + Files.Max(x => x.ReadResult.Extent.ZMax)) / 2, MidpointRounding.AwayFromZero);
    }
    private void RequestImport()
    {
        if (Files.Count == 0) { _dialog.Warn("입력 확인", "하나 이상의 SHP 파일을 추가하세요."); return; }
        RequestedImport = new PipeAlignmentImportRequest { Files = Files.Select(x => new PipeAlignmentImportFile(x.Path, x.PipeKind)).ToList(), ReferenceX = ReferenceX, ReferenceY = ReferenceY, ReferenceZ = ReferenceZ, ZSource = ZSource, ZDatum = ZDatum, ParseCombinedDiameter = ParseCombinedDiameter };
        CloseAction?.Invoke();
    }
}
