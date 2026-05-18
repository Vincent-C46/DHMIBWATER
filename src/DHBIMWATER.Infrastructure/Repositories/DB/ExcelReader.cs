using Autodesk.Revit.UI;
using DHBIMWATER.Application.DTOs.Revit.PumpingStation;
using DHBIMWATER.Application.Interfaces;
using ExcelDataReader;
using System.Data;
using System.IO;
using System.Text;

namespace DHBIMWATER.Infrastructure.Repositories.DB
{
    public class ExcelReader : IExcelReader
    {
        public IReadOnlyDictionary<string, List<string[]>> Read(string filePath)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding encoding1252 = Encoding.GetEncoding(1252);

            var dict = new Dictionary<string, List<string[]>>();

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                var dataSet = reader.AsDataSet();
                DataTable? dataTable = null;

                var sheets = dataSet.Tables;

                foreach (DataTable sheet in sheets)
                {
                    List<string[]> rows = sheet.Rows
                                          .Cast<DataRow>()
                                          .Select(row => row.ItemArray
                                          .Select(cell => cell?.ToString() ?? string.Empty)
                                          .ToArray())
                                          .ToList();
                    dict[sheet.TableName] = rows;
                }
            }
            return dict;
        }

        public List<string[]> Read(string filePath, string sheetName)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding encoding1252 = Encoding.GetEncoding(1252);

            var rows = new List<string[]>();

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                var dataSet = reader.AsDataSet();
                var sheets = dataSet.Tables;
                var sheet = sheets[sheetName];

                rows = sheet.Rows
                            .Cast<DataRow>()
                            .Select(row => row.ItemArray
                            .Select(cell => cell?.ToString() ?? string.Empty)
                            .ToArray())
                            .ToList();
            }
            return rows;
        }
    }
}
