using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.UseCases.AutoGenerator;
using DHBIMWATER.Revit.DependencyInjection;
using DHBIMWATER.UI.ViewModels.Modeling;
using DHBIMWATER.UI.Views.Modeling;
using System.Windows.Interop;

namespace DHBIMWATER.Revit.Commands;

[Transaction(TransactionMode.Manual)]
public class ValveRoomCommand : CommandBase
{
    protected override Result ExecuteInternal(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var view = ServiceContainer.GetService<ValveRoomView>();
        var viewModel = (ValveRoomViewModel)view.DataContext;
        new WindowInteropHelper(view).Owner = commandData.Application.MainWindowHandle;
        view.ShowDialog();

        if (viewModel.RequestedCreate is not { } request) return Result.Cancelled;
        ServiceContainer.GetService<CreateValveRoomUseCase>().Execute(request);
        return Result.Succeeded;
    }
}