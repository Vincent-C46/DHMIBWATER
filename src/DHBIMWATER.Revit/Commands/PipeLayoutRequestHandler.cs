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
    public Action<bool, string>? ModelCreationCompleted { get; set; }

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
                    var result = _useCase.Execute(network);
                    // 경고는 배치 실패가 아니다(트랜잭션은 이미 커밋됐다). 성공으로 알리되 본문을 함께 보여준다.
                    ModelCreationCompleted?.Invoke(true, result.Warnings.Count == 0
                        ? result.Summary
                        : $"{result.Summary} 확인 필요 {result.Warnings.Count}건.");
                    if (result.Warnings.Count > 0)
                        TaskDialog.Show("배관 생성 확인 필요", $"{result.Summary}\n\n- " + string.Join("\n- ", result.Warnings.Take(20)));
                    break;

                case PipeLayoutRequestId.PickOutline:
                    OutlinePicked?.Invoke(_outlinePick.PickExteriorWalls());
                    break;
            }
        }
        catch (Exception ex)
        {
            if (requestId == PipeLayoutRequestId.PickOutline) OutlinePicked?.Invoke(null);
            if (requestId == PipeLayoutRequestId.CreateModel) ModelCreationCompleted?.Invoke(false, ex.Message);
            TaskDialog.Show(requestId == PipeLayoutRequestId.PickOutline ? "외곽 벽체 선택 오류" : "배관 생성 오류", ex.Message);
        }
    }

    public string GetName() => "PipeLayoutRequest";
}
