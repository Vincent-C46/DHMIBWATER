using System.Collections.Generic;
using DHBIMWATER.Core.Parameters;

namespace DHBIMWATER.Infrastructure.Repositories.Mock
{
    public class MockSharedParameterRepository : ISharedParameterRepository
    {
        public void EnsureParameters(IReadOnlyList<SharedParameterDefinition> definitions)
        {
        }
    }
}
