using DHBIMWATER.Core.Structures;

namespace DHBIMWATER.Application.DTOs.Revit.PumpingStation;

public record PumpMaterialSpecDto(
    ConcreteSpec Wall,
    ConcreteSpec Slab,
    ConcreteSpec Foundation,
    ConcreteSpec Girder,
    ConcreteSpec Stair,
    ConcreteSpec LeanConcrete);
