using Autodesk.Revit.UI;
using DHBIMWATER.Application.UseCases.AutoGenerator;
using DHBIMWATER.Core.Piping;

namespace DHBIMWATER.Revit.Commands;

/// <summary>배관 생성 UseCase를 Revit API 컨텍스트에서 실행한다.</summary>
internal sealed class PipeLayoutRequestHandler : IExternalEventHandler
{
    private readonly CreateValvePipingUseCase _useCase;

    public PipeLayoutRequest Request { get; } = new();
    public PipeNetworkDefinition? PendingNetwork { get; set; }

    public PipeLayoutRequestHandler(CreateValvePipingUseCase useCase) => _useCase = useCase;

    public void Execute(UIApplication uiapp)
    {
        try
        {
            if (Request.Take() != PipeLayoutRequestId.CreateModel || PendingNetwork is null) return;
            var network = PendingNetwork;
            PendingNetwork = null;
            _useCase.Execute(network);
        }
        catch (Exception ex)
        {
            TaskDialog.Show("배관 생성 오류", ex.Message);
        }
    }

    public string GetName() => "PipeLayoutRequest";
}
