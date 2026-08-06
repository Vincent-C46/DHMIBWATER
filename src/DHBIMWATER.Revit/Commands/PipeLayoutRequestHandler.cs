using Autodesk.Revit.UI;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.UseCases.AutoGenerator;
using DHBIMWATER.Core.Piping;

namespace DHBIMWATER.Revit.Commands;

/// <summary>배관 생성 UseCase와 외곽 벽체 피킹을 Revit API 컨텍스트에서 실행한다.</summary>
internal sealed class PipeLayoutRequestHandler : IExternalEventHandler
{
    private readonly CreateValvePipingUseCase _useCase;
    private readonly IValveRoomOutlinePickRepo _outlinePick;

    public PipeLayoutRequest Request { get; } = new();
    public PipeNetworkDefinition? PendingNetwork { get; set; }

    /// <summary>피킹 결과를 ViewModel로 되돌리는 콜백. UI 스레드 마샬링은 호출측이 담당한다.</summary>
    public Action<ValveRoomOutline?>? OutlinePicked { get; set; }

    public PipeLayoutRequestHandler(CreateValvePipingUseCase useCase, IValveRoomOutlinePickRepo outlinePick)
    {
        _useCase = useCase;
        _outlinePick = outlinePick;
    }

    public void Execute(UIApplication uiapp)
    {
        var requestId = Request.Take();
        try
        {
            switch (requestId)
            {
                case PipeLayoutRequestId.CreateModel:
                    if (PendingNetwork is null) return;
                    var network = PendingNetwork;
                    PendingNetwork = null;
                    _useCase.Execute(network);
                    break;

                case PipeLayoutRequestId.PickOutline:
                    OutlinePicked?.Invoke(_outlinePick.PickExteriorWalls());
                    break;
            }
        }
        catch (Exception ex)
        {
            if (requestId == PipeLayoutRequestId.PickOutline) OutlinePicked?.Invoke(null);
            TaskDialog.Show(requestId == PipeLayoutRequestId.PickOutline ? "외곽 벽체 선택 오류" : "배관 생성 오류", ex.Message);
        }
    }

    public string GetName() => "PipeLayoutRequest";
}
