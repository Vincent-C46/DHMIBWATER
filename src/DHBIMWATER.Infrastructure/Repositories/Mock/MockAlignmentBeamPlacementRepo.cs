using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Core.Gis;
namespace DHBIMWATER.Infrastructure.Repositories.Mock;
internal sealed class MockAlignmentBeamPlacementRepo : IAlignmentBeamPlacementRepo
{
    public int PlaceAlong(IReadOnlyList<PipeAlignment> alignments, string beamTypeName, string? levelName, double intervalM, bool alignTangent, AlignmentPlacementOrigin origin,
        IReadOnlyList<IReadOnlyList<VertexTrim>>? trims = null, string? diameterParameterName = null, string? kindParameterName = null)
        => alignments.Select((x, i) => AlignmentIntervalSampler.SampleSegments(x.Vertices, intervalM, trims is not null && i < trims.Count ? trims[i] : null).Count).Sum();
}
