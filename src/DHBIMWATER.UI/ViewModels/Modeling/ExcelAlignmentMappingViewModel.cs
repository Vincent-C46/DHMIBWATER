using System.Collections.ObjectModel;
using System.Data;
using System.Windows.Input;
using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.UI.Base;
using DHBIMWATER.UI.Commands;

namespace DHBIMWATER.UI.ViewModels.Modeling;

public sealed class ExcelSheetPickItem : ViewModelBase
{
    private bool _isChecked;
    public required string Name { get; init; }
    public bool IsChecked { get => _isChecked; set { if (SetProperty(ref _isChecked, value)) CheckedChanged?.Invoke(); } }
    public Action? CheckedChanged { get; init; }
}

/// <summary>열 매핑 콤보박스 항목. Index는 0-based 열 번호(ExcelAlignmentMapping과 동일 기준)이고, 표시 텍스트에 열 문자를 함께 보여줘 헤더가 비었거나 중복돼도 구분할 수 있다.</summary>
public sealed record ExcelColumnOption(int Index, string Display)
{
    public override string ToString() => Display;
}

/// <summary>엑셀 좌표표(코리더 점 보고서 등)의 시트·헤더행·시작행·X·Y·Z·Station 열을 지정하는 대화상자.</summary>
public sealed class ExcelAlignmentMappingViewModel : ViewModelBase
{
    private readonly IExcelAlignmentSourceReader _reader;
    private readonly IDialogService _dialog;
    private int _headerRow = 1, _dataStartRow = 2;
    private ExcelSheetPickItem? _previewSheet;
    private ExcelColumnOption? _xColumn, _yColumn, _zColumn, _stationColumn, _diameterColumn, _kindColumn, _fittingColumn;
    private DataTable _previewTable = new();

    public ExcelAlignmentMappingViewModel(IExcelAlignmentSourceReader reader, IDialogService dialog, string filePath)
    {
        _reader = reader; _dialog = dialog; FilePath = filePath;
        foreach (var name in reader.GetSheetNames(filePath)) Sheets.Add(new ExcelSheetPickItem { Name = name, CheckedChanged = OnSheetCheckedChanged });
        PreviewSheet = Sheets.FirstOrDefault();
        OkCommand = new RelayCommand(_ => Confirm());
        CancelCommand = new RelayCommand(_ => CloseAction?.Invoke());
        RefreshPreview();
    }

    public string FilePath { get; }
    public string FileName => System.IO.Path.GetFileName(FilePath);
    public ObservableCollection<ExcelSheetPickItem> Sheets { get; } = new();
    public ObservableCollection<ExcelColumnOption> ColumnOptions { get; } = new();
    /// <summary>Station·직경·관종처럼 선택 사항인 열의 콤보박스가 공유하는 목록. 맨 앞에 "(사용 안 함)"을 둔다.</summary>
    public ObservableCollection<ExcelColumnOption> OptionalColumnOptions { get; } = new();
    public DataView PreviewView => _previewTable.DefaultView;

    public int HeaderRow { get => _headerRow; set { if (SetProperty(ref _headerRow, value)) RefreshPreview(); } }
    public int DataStartRow { get => _dataStartRow; set { if (SetProperty(ref _dataStartRow, value)) RefreshPreview(); } }
    public ExcelSheetPickItem? PreviewSheet { get => _previewSheet; set { if (SetProperty(ref _previewSheet, value)) RefreshPreview(); } }

    public ExcelColumnOption? XColumn { get => _xColumn; set { if (SetProperty(ref _xColumn, value)) OnPropertyChanged(nameof(CanConfirm)); } }
    public ExcelColumnOption? YColumn { get => _yColumn; set { if (SetProperty(ref _yColumn, value)) OnPropertyChanged(nameof(CanConfirm)); } }
    public ExcelColumnOption? ZColumn { get => _zColumn; set { if (SetProperty(ref _zColumn, value)) OnPropertyChanged(nameof(CanConfirm)); } }
    /// <summary>선택 사항. null이면 "(사용 안 함)".</summary>
    public ExcelColumnOption? StationColumn { get => _stationColumn; set => SetProperty(ref _stationColumn, value); }
    /// <summary>선택 사항. 지정하면 데이터 영역에서 처음 찾은 값을 관로 1개의 직경 필드값으로 쓴다. 메인 그리드에서는 "직경" 필드로 나타난다.</summary>
    public ExcelColumnOption? DiameterColumn { get => _diameterColumn; set => SetProperty(ref _diameterColumn, value); }
    /// <summary>선택 사항. DiameterColumn과 동일한 방식으로 관종을 읽는다. 메인 그리드에서는 "관종" 필드로 나타난다.</summary>
    public ExcelColumnOption? KindColumn { get => _kindColumn; set => SetProperty(ref _kindColumn, value); }
    /// <summary>선택 사항. DiameterColumn과 동일한 방식으로 피팅(이형관)명을 읽는다. 컬럼이 없으면 "(사용 안 함)"으로 둔다 — 배치 로직은 아직 없고 값만 보관한다.</summary>
    public ExcelColumnOption? FittingColumn { get => _fittingColumn; set => SetProperty(ref _fittingColumn, value); }

    public int CheckedCount => Sheets.Count(x => x.IsChecked);
    public bool CanConfirm => CheckedCount > 0 && XColumn is not null && YColumn is not null && ZColumn is not null;
    public string StatusText => $"체크한 시트 {CheckedCount}개" + (CanConfirm ? " · X·Y·Z 매핑 완료" : " · X·Y·Z 열을 모두 지정하세요");

    public ICommand OkCommand { get; }
    public ICommand CancelCommand { get; }
    public Action? CloseAction { get; set; }
    public IReadOnlyList<ExcelAlignmentMapping>? Result { get; private set; }

    private void OnSheetCheckedChanged()
    {
        OnPropertyChanged(nameof(CheckedCount)); OnPropertyChanged(nameof(CanConfirm)); OnPropertyChanged(nameof(StatusText));
        if (PreviewSheet is null || !PreviewSheet.IsChecked) PreviewSheet = Sheets.FirstOrDefault(x => x.IsChecked) ?? Sheets.FirstOrDefault();
    }

    private void RefreshPreview()
    {
        var previousX = XColumn?.Index; var previousY = YColumn?.Index; var previousZ = ZColumn?.Index;
        var previousStation = StationColumn?.Index; var previousDiameter = DiameterColumn?.Index; var previousKind = KindColumn?.Index; var previousFitting = FittingColumn?.Index;
        ColumnOptions.Clear(); OptionalColumnOptions.Clear();
        if (PreviewSheet is null || HeaderRow < 1) { _previewTable = new DataTable(); OnPropertyChanged(nameof(PreviewView)); XColumn = YColumn = ZColumn = StationColumn = DiameterColumn = KindColumn = FittingColumn = null; return; }

        IReadOnlyList<IReadOnlyList<string?>> rows;
        try { rows = _reader.PreviewRows(FilePath, PreviewSheet.Name, Math.Max(HeaderRow, DataStartRow) + 3); }
        catch (Exception ex) { _dialog.Warn("엑셀 미리보기", ex.Message); return; }

        // 미리보기는 엑셀처럼 열 문자(A, B, C ...)를 그대로 헤더로 쓴다 — 실제 헤더 텍스트는 중복·공백일 수 있어 DataTable 열 이름으로 못 쓴다.
        var table = new DataTable();
        table.Columns.Add("#");
        var columnCount = rows.Count == 0 ? 0 : rows.Max(r => r.Count);
        for (var c = 0; c < columnCount; c++) table.Columns.Add(ColumnLetter(c));
        for (var i = 0; i < rows.Count; i++)
        {
            var rowNumber = i + 1;
            var row = table.NewRow();
            row[0] = rowNumber == HeaderRow ? $"{rowNumber} (헤더)" : rowNumber.ToString();
            for (var c = 0; c < rows[i].Count; c++) row[c + 1] = rows[i][c];
            table.Rows.Add(row);
        }
        _previewTable = table;
        OnPropertyChanged(nameof(PreviewView));

        if (HeaderRow <= rows.Count)
        {
            var header = rows[HeaderRow - 1];
            for (var c = 0; c < header.Count; c++)
            {
                var text = string.IsNullOrWhiteSpace(header[c]) ? $"(빈 열 {ColumnLetter(c)})" : header[c];
                ColumnOptions.Add(new ExcelColumnOption(c, $"{text} ({ColumnLetter(c)})"));
            }
        }
        OptionalColumnOptions.Add(new ExcelColumnOption(-1, "(사용 안 함)"));
        foreach (var option in ColumnOptions) OptionalColumnOptions.Add(option);

        XColumn = ColumnOptions.FirstOrDefault(x => x.Index == previousX);
        YColumn = ColumnOptions.FirstOrDefault(x => x.Index == previousY);
        ZColumn = ColumnOptions.FirstOrDefault(x => x.Index == previousZ);
        StationColumn = OptionalColumnOptions.FirstOrDefault(x => x.Index == previousStation) ?? OptionalColumnOptions.First();
        DiameterColumn = OptionalColumnOptions.FirstOrDefault(x => x.Index == previousDiameter) ?? OptionalColumnOptions.First();
        KindColumn = OptionalColumnOptions.FirstOrDefault(x => x.Index == previousKind) ?? OptionalColumnOptions.First();
        FittingColumn = OptionalColumnOptions.FirstOrDefault(x => x.Index == previousFitting) ?? OptionalColumnOptions.First();
        OnPropertyChanged(nameof(CanConfirm)); OnPropertyChanged(nameof(StatusText));
    }

    private void Confirm()
    {
        if (CheckedCount == 0) { _dialog.Warn("입력 확인", "시트를 1개 이상 선택하세요."); return; }
        if (HeaderRow < 1) { _dialog.Warn("입력 확인", "헤더 행은 1 이상이어야 합니다."); return; }
        if (DataStartRow <= HeaderRow) { _dialog.Warn("입력 확인", "데이터 시작 행은 헤더 행보다 커야 합니다."); return; }
        if (XColumn is null || YColumn is null || ZColumn is null) { _dialog.Warn("입력 확인", "X, Y, Z 열은 필수입니다."); return; }
        var station = StationColumn is { Index: >= 0 } ? StationColumn.Index : (int?)null;
        var diameter = DiameterColumn is { Index: >= 0 } ? DiameterColumn.Index : (int?)null;
        var kind = KindColumn is { Index: >= 0 } ? KindColumn.Index : (int?)null;
        var fitting = FittingColumn is { Index: >= 0 } ? FittingColumn.Index : (int?)null;
        Result = Sheets.Where(x => x.IsChecked)
            .Select(x => new ExcelAlignmentMapping(x.Name, HeaderRow, DataStartRow, XColumn.Index, YColumn.Index, ZColumn.Index, station, diameter, kind, fitting))
            .ToList();
        CloseAction?.Invoke();
    }

    private static string ColumnLetter(int index)
    {
        var letter = string.Empty; var n = index;
        do { letter = (char)('A' + n % 26) + letter; n = n / 26 - 1; } while (n >= 0);
        return letter;
    }
}
