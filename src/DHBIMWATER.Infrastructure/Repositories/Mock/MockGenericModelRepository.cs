using DHBIMWATER.Application.Interfaces;

namespace DHBIMWATER.Infrastructure.Repositories.Mock
{
    public class MockGenericModelRepository : IGenericModelRepository
    {
        public IEnumerable<object> GetAll()
        {
            return new List<object>
        {
            new object(),
            new object(),
            new object(),
            new object(),
        };
        }
    }
}
