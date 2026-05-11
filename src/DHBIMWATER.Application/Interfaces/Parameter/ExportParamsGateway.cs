using DHBIMWATER.Application.DTOs.Revit;

namespace DHBIMWATER.Application.Interfaces.Parameter
{
    public interface IExportParamsGateway
    {
        IReadOnlyList<CategoryInfo> GetCategories();
        IReadOnlyList<string> GetParameters(string categoryKey);
        void Export(string categoryKey, IList<string> paramNames, string filePath);
    }
}
