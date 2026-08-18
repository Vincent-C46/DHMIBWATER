using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Application.Interfaces.Gis;

namespace DHBIMWATER.Infrastructure.Repositories.Mock;

/// <summary>Revit 없이 동작하는 스텁. Sandbox/테스트 환경에서만 쓴다.</summary>
public sealed class MockAdaptiveBendPlacementRepo : IAdaptiveBendPlacementRepo
{
    public AdaptiveBendPlacementResult Place(IReadOnlyList<AdaptiveBendPlacementPlan> plans, AlignmentPlacementOrigin origin, PipeInfoParameterContext? info = null) => new(plans.Count, Array.Empty<string>());

    public IReadOnlyList<(string FamilyName, string TypeName)> FindMissingSymbols(IEnumerable<(string FamilyName, string TypeName)> pairs) => Array.Empty<(string, string)>();
}
