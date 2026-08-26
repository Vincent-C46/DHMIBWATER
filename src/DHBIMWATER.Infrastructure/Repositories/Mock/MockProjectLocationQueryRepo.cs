using DHBIMWATER.Application.Interfaces;

namespace DHBIMWATER.Infrastructure.Repositories.Mock;

internal sealed class MockProjectLocationQueryRepo : IProjectLocationQueryRepo
{
    public (double EastWestMeters, double NorthSouthMeters) GetProjectBasePointSharedPosition() => (0, 0);
}
