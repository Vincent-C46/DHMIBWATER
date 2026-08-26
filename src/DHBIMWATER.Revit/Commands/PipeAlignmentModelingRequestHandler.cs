using Autodesk.Revit.UI;
using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Application.UseCases.Gis;

namespace DHBIMWATER.Revit.Commands;

internal enum PipeAlignmentModelingRequestId { None, CreateModel }

internal sealed class PipeAlignmentModelingRequest
{
    private int _requestId;

    public PipeAlignmentModelingRequestId Take() =>
        (PipeAlignmentModelingRequestId)Interlocked.Exchange(ref _requestId, (int)PipeAlignmentModelingRequestId.None);

    public void Make(PipeAlignmentModelingRequestId requestId) =>
        Interlocked.Exchange(ref _requestId, (int)requestId);
}

internal sealed class PipeAlignmentModelingRequestHandler : IExternalEventHandler
{
    private readonly ModelPipeAlignmentUseCase _useCase;

    public PipeAlignmentModelingRequest Request { get; } = new();
    public Application.DTOs.Gis.PipeAlignmentModelingRequest? PendingRequest { get; set; }
    public Action<PipeAlignmentProgress>? ProgressReported { get; set; }
    public Action<PipeAlignmentModelingResult>? Completed { get; set; }
    public Action<string>? Failed { get; set; }

    public PipeAlignmentModelingRequestHandler(ModelPipeAlignmentUseCase useCase) => _useCase = useCase;

    public void Execute(UIApplication app)
    {
        if (Request.Take() != PipeAlignmentModelingRequestId.CreateModel || PendingRequest is not { } request) return;
        PendingRequest = null;

        try
        {
            var progress = new DelegateProgress<PipeAlignmentProgress>(value => ProgressReported?.Invoke(value));
            Completed?.Invoke(_useCase.Execute(request, progress));
        }
        catch (Exception ex)
        {
            Failed?.Invoke(ex.Message);
        }
    }

    public string GetName() => "PipeAlignmentModelingRequest";

    private sealed class DelegateProgress<T> : IProgress<T>
    {
        private readonly Action<T> _report;
        public DelegateProgress(Action<T> report) => _report = report;
        public void Report(T value) => _report(value);
    }
}
