namespace DHBIMWATER.Core.Piping;

public sealed class PipeEdge
{
    private readonly List<InlineFitting> _inlineFittings = [];

    public PipeEdge(Guid id, Guid startNodeId, Guid endNodeId, IEnumerable<InlineFitting>? inlineFittings = null)
    {
        Id = id;
        StartNodeId = startNodeId;
        EndNodeId = endNodeId;
        if (inlineFittings is not null) _inlineFittings.AddRange(inlineFittings.OrderBy(x => x.Order));
    }

    public Guid Id { get; }
    public Guid StartNodeId { get; }
    public Guid EndNodeId { get; }
    public IReadOnlyList<InlineFitting> InlineFittings => _inlineFittings;

    internal void AddFitting(InlineFitting fitting) => _inlineFittings.Add(fitting);
    internal void ReplaceFittings(IEnumerable<InlineFitting> fittings)
    {
        _inlineFittings.Clear();
        _inlineFittings.AddRange(fittings.OrderBy(x => x.Order));
    }
}
