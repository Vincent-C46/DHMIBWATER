using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Application.Interfaces.Gis;

namespace DHBIMWATER.Infrastructure.Repositories.Mock;

/// <summary>Revit 없이 동작하는 스텁. Sandbox/테스트 환경에서만 쓴다.</summary>
public sealed class MockAdaptiveBendPlacementRepo : IAdaptiveBendPlacementRepo
{
    public int Place(IReadOnlyList<AdaptiveBendPlacementPlan> plans, AlignmentPlacementOrigin origin) => plans.Count;
}
