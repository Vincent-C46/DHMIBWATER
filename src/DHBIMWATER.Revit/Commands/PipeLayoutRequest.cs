using System.Threading;

namespace DHBIMWATER.Revit.Commands;

internal enum PipeLayoutRequestId { None, CreateModel }

internal sealed class PipeLayoutRequest
{
    private int _requestId;

    public PipeLayoutRequestId Take() => (PipeLayoutRequestId)Interlocked.Exchange(ref _requestId, (int)PipeLayoutRequestId.None);
    public void Make(PipeLayoutRequestId requestId) => Interlocked.Exchange(ref _requestId, (int)requestId);
}
