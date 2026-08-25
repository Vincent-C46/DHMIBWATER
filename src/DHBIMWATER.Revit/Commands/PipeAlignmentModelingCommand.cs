using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.UseCases.Gis;
using DHBIMWATER.Revit.DependencyInjection;
using DHBIMWATER.UI.ViewModels.Modeling;
using DHBIMWATER.UI.Views.Modeling;
using System.Windows.Threading;

namespace DHBIMWATER.Revit.Commands;

[Transaction(TransactionMode.Manual)]
public class PipeAlignmentModelingCommand : ModelessCommandBase
{
    protected override Result ExecuteInternal(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        // 생성자는 Revit API 조회를 포함하므로 반드시 Revit 메인 스레드에서 해석한다.
        var viewModel = ServiceContainer.GetService<PipeAlignmentModelingViewModel>();
        var handler = new PipeAlignmentModelingRequestHandler(ServiceContainer.GetService<ModelPipeAlignmentUseCase>());
        var externalEvent = ExternalEvent.Create(handler);
        var revitHandle = commandData.Application.MainWindowHandle;
        PipeAlignmentModelingView? view = null;

        viewModel.CreateModelAction = request =>
        {
            handler.PendingRequest = request;
            handler.Request.Make(PipeAlignmentModelingRequestId.CreateModel);
            var status = externalEvent.Raise();
            if (status == ExternalEventRequest.Accepted) return;

            handler.PendingRequest = null;
            viewModel.ApplyFailure($"Revit 요청을 등록하지 못했습니다: {status}");
        };

        handler.ProgressReported = progress => view?.Dispatcher.InvokeAsync(() => viewModel.ApplyProgress(progress));
        handler.Completed = result => view?.Dispatcher.InvokeAsync(() => viewModel.ApplyResult(result));
        handler.Failed = error => view?.Dispatcher.InvokeAsync(() => viewModel.ApplyFailure(error));

        var uiThread = new Thread(() =>
        {
            try
            {
                view = new PipeAlignmentModelingView(viewModel);
                view.Closed += (_, _) =>
                {
                    externalEvent.Dispose();
                    ServiceContainer.Dispose();
                    view.Dispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
                };
                AttachModelessWindow(view, revitHandle);
                view.Show();
                Dispatcher.Run();
            }
            catch (Exception ex)
            {
                if (view is not null) ModelessWindowGate.Close(view);
                externalEvent.Dispose();
                ServiceContainer.Dispose();
                // TaskDialog는 Revit 메인 스레드 전용이므로 전용 STA 스레드에서 호출하지 않는다.
                System.Windows.MessageBox.Show($"관로 모델링 창을 여는 중 오류가 발생했습니다.\n{ex.Message}", "DHBIMWATER");
            }
        });

        uiThread.SetApartmentState(ApartmentState.STA);
        uiThread.Start();
        return Result.Succeeded;
    }
}
