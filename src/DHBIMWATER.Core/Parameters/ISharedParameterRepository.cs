using System.Collections.Generic;

namespace DHBIMWATER.Core.Parameters
{
    public interface ISharedParameterRepository
    {
        void EnsureParameters(IReadOnlyList<SharedParameterDefinition> definitions);
    }
}
