using DHBIMWATER.Application.DTOs.Revit.ValveRoom;

namespace DHBIMWATER.Application.Interfaces;

/// <summary>공기밸브실 독립기초의 면에 Void 패밀리를 호스팅한다.</summary>
public interface IAirValveVoidCommandRepo
{
    void CreateAirValveFoundationVoid(int foundationElementId, AirValveVoidPlacementDefinition definition);
}
