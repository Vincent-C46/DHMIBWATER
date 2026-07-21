using DHBIMWATER.Core.Gis;
namespace DHBIMWATER.Application.Interfaces.Gis;
public interface IAlignmentBeamPlacementRepo
{
    int PlaceAlong(IReadOnlyList<PipeAlignment> alignments, string beamTypeName, string? levelName, double intervalM, bool alignTangent);
}
