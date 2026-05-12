using System.Collections.Generic;

namespace DHBIMWATER.Application.Interfaces.Parameter
{
    public interface IImportParamsGateway
    {
        Dictionary<int, Dictionary<string, string>> Load(string filePath);

        int Apply(
            object context,
            Dictionary<int, Dictionary<string, string>> map,
            bool overwriteExisting);
    }
}
