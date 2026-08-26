using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Piping;

namespace DHBIMWATER.Application.UseCases.AutoGenerator;

public sealed class CreateValvePipingUseCase
{
    private readonly ITransactionContext _transaction;
    private readonly IReadOnlyDictionary<PipeOutputMode, IPipeCommandRepo> _repositories;

    public CreateValvePipingUseCase(ITransactionContext transaction, IEnumerable<IPipeCommandRepo> repositories)
    {
        _transaction = transaction;
        _repositories = repositories.ToDictionary(x => x.OutputMode);
    }

    public PipeCreationResult Execute(PipeNetworkDefinition network)
    {
        if (network.Edges.Count == 0) throw new InvalidOperationException("생성할 배관 세그먼트가 없습니다.");
        if (!_repositories.TryGetValue(network.OutputMode, out var repository))
            throw new InvalidOperationException($"{network.OutputMode} 배관 출력 구현이 등록되지 않았습니다.");

        using (_transaction)
        {
            try
            {
                _transaction.Begin("Create Valve Room Piping");
                var result = repository.CreateNetwork(network);
                _transaction.Commit();
                return result;
            }
            catch
            {
                _transaction.Rollback();
                throw;
            }
        }
    }
}
