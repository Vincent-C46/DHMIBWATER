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
        private readonly XLWorkbook _workbook = new();
        private IXLWorksheet _currentSheet;
        private int _currentRow = 1;
        private IXLWorksheet CurrentSheet =>
        _currentSheet ?? throw new InvalidOperationException("CreateSheet을 먼저 호출하세요.");

        public void CreateSheet(string sheetName)
        {
            _currentSheet = _workbook.Worksheets.Add(sheetName);
            _currentRow = 1;
        }
        public void WriteHeader(IEnumerable<string> columns)
        {
            throw new NotImplementedException();
        }

        public void WriteRow(IEnumerable<string> values)
        {
            throw new NotImplementedException();
        }

        public void WriteTotalRow(IEnumerable<string> values)
        {
            throw new NotImplementedException();
        }

        public void Save(string filePath)
        {
            throw new NotImplementedException();
        }

        public void WriteEmptyRow()
        {
            throw new NotImplementedException();
        }

    }
}
