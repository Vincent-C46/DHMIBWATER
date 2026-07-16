using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Revit.DependencyInjection;
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
        var handler = new PipeLayoutRequestHandler(useCase);
        var externalEvent = ExternalEvent.Create(handler);
        ((DHBIMWATER.UI.ViewModels.Modeling.PipeLayoutViewModel)view.DataContext).CreateModelAction = network =>
        {
            handler.PendingNetwork = network;
            handler.Request.Make(PipeLayoutRequestId.CreateModel);
            externalEvent.Raise();
        };
        new WindowInteropHelper(view).Owner = commandData.Application.MainWindowHandle;
        view.Show();
        return Result.Succeeded;
    }
}
