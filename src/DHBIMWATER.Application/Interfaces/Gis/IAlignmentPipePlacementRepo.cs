using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Core.Gis;
namespace DHBIMWATER.Application.Interfaces.Gis;
public interface IAlignmentPipePlacementRepo
{
    int PlaceAlong(IReadOnlyList<PipeAlignment> alignments, string pipingSystemTypeName, string pipeTypeName, string? levelName, double intervalM, AlignmentPlacementOrigin origin);
}
