using DHBIMWATER.Application.DTOs.Revit;
using DHBIMWATER.Application.Interfaces.Parameter;

namespace DHBIMWATER.Application.UseCases.Parameter
{
    public interface IExportParamsUseCase
    {
        IReadOnlyList<CategoryInfo> GetCategories();
        IReadOnlyList<string> GetParameters(string categoryKey);
        void Export(string categoryKey, IList<string> paramNames, string filePath);
    }

    public class ExportParamsUseCase : IExportParamsUseCase
    {
        private readonly IExportParamsGateway _gateway;

        public ExportParamsUseCase(IExportParamsGateway gateway)
        {
            _gateway = gateway;
        }

        public IReadOnlyList<CategoryInfo> GetCategories()
            => _gateway.GetCategories();

        public IReadOnlyList<string> GetParameters(string categoryKey)
            => _gateway.GetParameters(categoryKey);

        public void Export(string categoryKey, IList<string> paramNames, string filePath)
            => _gateway.Export(categoryKey, paramNames, filePath);
    }
}
