using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Structures;

namespace DHBIMWATER.Infrastructure.Repositories.Mock;

public sealed class MockFoundationCommandRepo : IFoundationCommandRepo
{
    public int CreateFoundationFromFirstInstance(FoundationDefinition definition) => 0;
}
