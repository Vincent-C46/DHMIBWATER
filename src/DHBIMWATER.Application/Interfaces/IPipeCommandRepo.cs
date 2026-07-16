using DHBIMWATER.Core.Piping;

namespace DHBIMWATER.Application.Interfaces;

/// <summary>Revit 배관 출력 구현(MEP Pipe 또는 GenericModel)의 공통 경계.</summary>
public interface IPipeCommandRepo
{
    PipeOutputMode OutputMode { get; }
    void CreateNetwork(PipeNetworkDefinition network);
}
