using System.Reflection.Metadata;
using DHBIMWATER.Application.Interfaces.Parameter;


namespace DHBIMWATER.Application.UseCases.Parameter
{
    public class ImportParamsUseCase : IImportParamsUseCase
    {
        private readonly IImportParamsGateway _gateway;

        public ImportParamsUseCase(IImportParamsGateway gateway)
        {
            _gateway = gateway;
        }

        public int Execute(object context, string filePath, bool overwriteExisting)
        {
            var map = _gateway.Load(filePath);
            if (map == null || map.Count == 0) return 0;

            return _gateway.Apply(context, map, overwriteExisting);
        }
    }
}
