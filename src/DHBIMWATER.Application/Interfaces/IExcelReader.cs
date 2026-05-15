using DHBIMWATER.Application.DTOs.Revit.PumpingStation;

namespace DHBIMWATER.Application.Interfaces
{
    public interface IExcelReader
    {
        IReadOnlyDictionary<string, List<string[]>> Read(string filePath);
        List<string[]> Read(string filePath, string sheetName);
    }
}
