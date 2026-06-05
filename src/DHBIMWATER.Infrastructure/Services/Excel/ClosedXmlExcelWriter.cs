using ClosedXML.Excel;
using DHBIMWATER.Application.Interfaces.Quantity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Infrastructure.Services.Excel
{
    public class ClosedXmlExcelWriter : IExcelExporter
    {
        #region Fields
        private readonly XLWorkbook _workbook = new();
        private IXLWorksheet _currentSheet;
        private int _currentRow = 1;
        private IXLWorksheet CurrentSheet =>
        _currentSheet ?? throw new InvalidOperationException("CreateSheet을 먼저 호출하세요.");
        #endregion

        public void CreateSheet(string sheetName)
        {
            _currentSheet = _workbook.Worksheets.Add(sheetName);
            _currentRow = 1;
        }
        public void WriteHeader(IEnumerable<string> columns)
        {
            var cols = columns.ToList();
            for (int i = 0; i < cols.Count; i++)
                CurrentSheet.Cell(_currentRow, i + 1).Value = cols[i];

            var range = CurrentSheet.Range(_currentRow, 1, _currentRow, cols.Count);
            range.Style.Font.Bold = true;
            range.Style.Fill.BackgroundColor = XLColor.LightGray;
            _currentRow++;
        }
        public void WriteRow(IEnumerable<string> values)
        {
            var vals = values.ToList();
            for (int i = 0; i < vals.Count; i++)
                CurrentSheet.Cell(_currentRow, i + 1).Value = vals[i];
            _currentRow++;
        }
        public void WriteTotalRow(IEnumerable<string> values)
        {
            var vals = values.ToList();
            for (int i = 0; i < vals.Count; i++)
                CurrentSheet.Cell(_currentRow, i + 1).Value = vals[i];

            var range = CurrentSheet.Range(_currentRow, 1, _currentRow, vals.Count);
            range.Style.Font.Bold = true;
            range.Style.Fill.BackgroundColor = XLColor.LightYellow;
            _currentRow++;
        }
        public void Save(string filePath)
        {
            _workbook.SaveAs(filePath);
        }
        public void WriteEmptyRow()
        {
            _currentRow++;
        }
    }
}
