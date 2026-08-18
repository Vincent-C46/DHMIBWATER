using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Core.Gis;
namespace DHBIMWATER.Infrastructure.Repositories.Mock;
internal sealed class MockAlignmentStraightPlacementRepo : IAlignmentStraightPlacementRepo
{
    public AlignmentStraightPlacementResult PlaceAlong(IReadOnlyList<PipeAlignment> alignments, string straightFamilyTypeName, double intervalM, AlignmentPlacementOrigin origin,
        StraightPipeSpecTable specs, IReadOnlyList<IReadOnlyList<VertexTrim>>? trims = null, string? diameterParameterName = null, string? kindParameterName = null,
        string? outerDiameterParameterName = null, string? thicknessParameterName = null, PipeInfoParameterContext? info = null)
        => new(alignments.Select((x, i) => AlignmentIntervalSampler.SampleSegments(x.Vertices, intervalM, trims is not null && i < trims.Count ? trims[i] : null).Count).Sum(), Array.Empty<string>());
}
