using System.Reflection.Metadata;

namespace DHBIMWATER.Application.UseCases.Parameter
{
    public interface IImportParamsUseCase
    {
        int Execute(object context, string filePath, bool overwriteExisting);
    }
}
