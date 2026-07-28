using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Core.Gis;
namespace DHBIMWATER.Infrastructure.Repositories.Mock;
internal sealed class MockAlignmentPipePlacementRepo : IAlignmentPipePlacementRepo
{
    public int PlaceAlong(IReadOnlyList<PipeAlignment> alignments, string pipingSystemTypeName, string pipeTypeName, string? levelName, double intervalM, AlignmentPlacementOrigin origin) => alignments.Sum(x => AlignmentIntervalSampler.SampleSegments(x.Vertices, intervalM).Count);
}
