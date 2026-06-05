using DHBIMWATER.Core.Quantity;

namespace DHBIMWATER.Application.Interfaces.Quantity
{
    public interface IExcelExporter
    {
        void CreateSheet(string sheetName);
        void WriteHeader(IEnumerable<string> columns);
        void WriteRow(IEnumerable<string> values);
        void WriteTotalRow(IEnumerable<string> values);
        void WriteEmptyRow();
        void Save(string filePath);
    }
}
