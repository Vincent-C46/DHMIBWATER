using DHBIMWATER.Application.Interface;

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
