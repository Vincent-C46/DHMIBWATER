using DHBIMWATER.Application.DTOs.Gis;

namespace DHBIMWATER.Application.Interfaces.Gis;

public interface IPipeAlignmentCommandRepo
{
    PipeAlignmentCreateResult Create(PipeAlignmentCreateDefinition definition, IProgress<PipeAlignmentProgress>? progress = null);
}
