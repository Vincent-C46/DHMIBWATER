using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.Interfaces.Geometry;

namespace DHBIMWATER.Application.UseCases.QuantityCalculator;

public sealed class VisualizeNetFacesUseCase
{
    private readonly ITransactionContext _transaction;
    private readonly INetFaceVisualizerRepo _repo;

    public VisualizeNetFacesUseCase(ITransactionContext transaction, INetFaceVisualizerRepo repo)
    {
        _transaction = transaction;
        _repo = repo;
    }

    public int Execute(IReadOnlyList<long> elementIds)
    {
        using (_transaction)
        {
            try
            {
                _transaction.Begin("Visualize Net Faces");
                var count = _repo.Visualize(elementIds);
                _transaction.Commit();
                return count;
            }
            catch
            {
                _transaction.Rollback();
                throw;
            }
        }
    }
}
