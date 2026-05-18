using DHBIMWATER.Application.DTOs.Revit.PumpingStation;
using DHBIMWATER.Application.Interfaces;
using ExcelDataReader;
using System.IO;
using System.Text;

namespace DHBIMWATER.Infrastructure.Repositories.Revit
{
    public class RevitExcelPumpReader : IExcelReader
    {
        public IEnumerable<PumpExcelDto> Read(string filePath, string sheetName)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            // 해당 시트로 이동
            do
            {
                if (reader.Name == sheetName) break;
            } while (reader.NextResult());

            if (reader.Name != sheetName)
                yield break;

            var result = new List<PumpExcelDto>();

            // 헤더 행 파싱 (양정고 목록)
            // TODO: 양정고 헤더 파싱

            // 4행 묶음 파싱
            // TODO: 펌프구경 / 개구부형상 / B5 / B6 / 받침블록(B,H) 파싱

            foreach (var dto in result)
                yield return dto;
        }
    }
}
