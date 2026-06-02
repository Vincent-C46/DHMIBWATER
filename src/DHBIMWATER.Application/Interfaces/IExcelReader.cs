using DHBIMWATER.Application.DTOs.Revit.PumpingStation;

namespace DHBIMWATER.Application.Interfaces
{
    public interface IExcelReader
    {
        IEnumerable<PumpExcelDto> Read(string filePath, string sheetName);
    }
}
