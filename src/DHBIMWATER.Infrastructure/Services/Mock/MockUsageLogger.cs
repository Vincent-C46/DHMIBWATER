using DHBIMWATER.Application.Interfaces;

namespace DHBIMWATER.Infrastructure.Services.Mock
{
    internal class MockUsageLogger : IUsageLogger
    {
        public Task LogAsync() => Task.CompletedTask;
    }
}
