using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.UI.ViewModels.Modeling;
using Xunit;

namespace DHBIMWATER.UI.Tests.ViewModels.Modeling;

public sealed class ExcelAlignmentMappingViewModelTests
{
    [Fact]
    public void CheckingSheet_FiltersPreviewAndGuessesCoordinateColumns()
    {
        var vm = new ExcelAlignmentMappingViewModel(new StubReader(), new StubDialog(), "sample.xlsx");

        Assert.Empty(vm.CheckedSheets);
        Assert.Null(vm.PreviewSheet);

        vm.Sheets[0].IsChecked = true;

        Assert.Single(vm.CheckedSheets);
        Assert.Same(vm.Sheets[0], vm.PreviewSheet);
        Assert.Equal(0, vm.XColumn?.Index);
        Assert.Equal(1, vm.YColumn?.Index);
        Assert.Equal(2, vm.ZColumn?.Index);
        Assert.Equal(3, vm.StationColumn?.Index);
    }

    [Fact]
    public void RefreshPreview_PreservesUserSelectedColumn()
    {
        var vm = new ExcelAlignmentMappingViewModel(new StubReader(), new StubDialog(), "sample.xlsx");
        vm.Sheets[0].IsChecked = true;
        vm.XColumn = vm.ColumnOptions.Single(x => x.Index == 4);

        vm.DataStartRow = 3;

        Assert.Equal(4, vm.XColumn?.Index);
    }

    [Fact]
    public void UncheckingLastSheet_ClearsPreviewSelection()
    {
        var vm = new ExcelAlignmentMappingViewModel(new StubReader(), new StubDialog(), "sample.xlsx");
        vm.Sheets[0].IsChecked = true;

        vm.Sheets[0].IsChecked = false;

        Assert.Empty(vm.CheckedSheets);
        Assert.Null(vm.PreviewSheet);
        Assert.False(vm.CanConfirm);
    }

    private sealed class StubReader : IExcelAlignmentSourceReader
    {
        public bool CanRead(string filePath) => true;
        public IReadOnlyList<string> GetSheetNames(string filePath) => ["관로1", "관로2"];
        public IReadOnlyList<IReadOnlyList<string?>> PreviewRows(string filePath, string sheetName, int maxRows) =>
        [
            [" East ", "NORTH", "표 고", "STA", "사용자 X"],
            ["1", "2", "3", "0+000", "4"],
            ["5", "6", "7", "0+010", "8"]
        ];
        public ShapefileReadResult Read(string filePath) => throw new NotSupportedException();
        public ShapefileReadResult Read(string filePath, ExcelAlignmentMapping mapping) => throw new NotSupportedException();
    }

    private sealed class StubDialog : IDialogService
    {
        public void Info(string title, string message) { }
        public void Warn(string title, string message) { }
        public bool Confirm(string title, string message) => true;
    }
}
