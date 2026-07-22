using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Core.Gis;
namespace DHBIMWATER.Infrastructure.Repositories.Mock;
internal sealed class MockAlignmentBeamPlacementRepo : IAlignmentBeamPlacementRepo
{
    public int PlaceAlong(IReadOnlyList<PipeAlignment> alignments, string beamTypeName, string? levelName, double intervalM, bool alignTangent) => alignments.Sum(x => AlignmentIntervalSampler.SampleSegments(x.Vertices, intervalM).Count);
}
