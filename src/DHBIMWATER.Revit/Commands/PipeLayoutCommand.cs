using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Revit.DependencyInjection;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.UseCases.AutoGenerator;
using DHBIMWATER.UI.Views.Modeling;
using System.Windows.Interop;

namespace DHBIMWATER.Revit.Commands;

[Transaction(TransactionMode.Manual)]
public class PipeLayoutCommand : CommandBase
{
    protected override Result ExecuteInternal(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var view = ServiceContainer.GetService<PipeLayoutView>();
        var useCase = ServiceContainer.GetService<CreateValvePipingUseCase>();
        var outlinePick = ServiceContainer.GetService<IValveRoomOutlinePickRepo>();
        var handler = new PipeLayoutRequestHandler(useCase, outlinePick);
        var externalEvent = ExternalEvent.Create(handler);
        var viewModel = (DHBIMWATER.UI.ViewModels.Modeling.PipeLayoutViewModel)view.DataContext;

        viewModel.CreateModelAction = network =>
        {
            handler.PendingNetwork = network;
            handler.Request.Make(PipeLayoutRequestId.CreateModel);
            externalEvent.Raise();
        };

        // 피킹 중에는 Revit 뷰를 봐야 하므로 창을 숨기고, 결과를 받은 뒤 UI 스레드에서 다시 띄운다.
        viewModel.PickOutlineAction = () =>
        {
            view.Hide();
            handler.Request.Make(PipeLayoutRequestId.PickOutline);
            externalEvent.Raise();
        };
        handler.OutlinePicked = outline => view.Dispatcher.Invoke(() =>
        {
            view.Show();
            view.Activate();
            viewModel.ApplyOutline(outline);
        });

        new WindowInteropHelper(view).Owner = commandData.Application.MainWindowHandle;
        view.Show();
        return Result.Succeeded;
    }
}
