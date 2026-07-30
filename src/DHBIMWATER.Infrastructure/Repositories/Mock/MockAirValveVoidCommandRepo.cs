using DHBIMWATER.Application.DTOs.Revit.ValveRoom;
using DHBIMWATER.Application.Interfaces;

namespace DHBIMWATER.Infrastructure.Repositories.Mock;

public sealed class MockAirValveVoidCommandRepo : IAirValveVoidCommandRepo
{
    public void CreateAirValveFoundationVoid(int foundationElementId, AirValveVoidPlacementDefinition definition) { }
}
