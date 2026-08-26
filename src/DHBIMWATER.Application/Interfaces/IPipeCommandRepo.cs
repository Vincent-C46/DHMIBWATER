using DHBIMWATER.Core.Piping;

namespace DHBIMWATER.Application.Interfaces;

/// <summary>배관 생성 결과 요약. 경고가 있어도 배치 자체는 성공한 것이므로 예외로 던지지 않는다
/// — 예외를 던지면 UseCase가 트랜잭션을 롤백해 배치가 통째로 사라진다.</summary>
public sealed record PipeCreationResult(string Summary, IReadOnlyList<string> Warnings)
{
    public PipeCreationResult(string summary) : this(summary, []) { }
}

/// <summary>Revit 배관 출력 구현(MEP Pipe / GenericModel / 직관·단관 패밀리)의 공통 경계.</summary>
public interface IPipeCommandRepo
{
    PipeOutputMode OutputMode { get; }
    PipeCreationResult CreateNetwork(PipeNetworkDefinition network);
}
