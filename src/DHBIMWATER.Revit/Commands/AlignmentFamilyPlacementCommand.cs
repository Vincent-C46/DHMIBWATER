using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.UseCases.Gis;
using DHBIMWATER.Revit.DependencyInjection;
using DHBIMWATER.UI.ViewModels.Modeling;
using DHBIMWATER.UI.Views.Modeling;
using System.Windows.Interop;

namespace DHBIMWATER.Revit.Commands;

[Transaction(TransactionMode.Manual)]
public class AlignmentFamilyPlacementCommand : CommandBase
{
    protected override Result ExecuteInternal(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var view = ServiceContainer.GetService<AlignmentFamilyPlacementView>();
        var viewModel = (AlignmentFamilyPlacementViewModel)view.DataContext;
        new WindowInteropHelper(view).Owner = commandData.Application.MainWindowHandle;
        view.ShowDialog();
        if (viewModel.RequestedPlacement is not { } request) return Result.Cancelled;
        var result = ServiceContainer.GetService<PlaceAlignmentFamilyUseCase>().Execute(request);
        TaskDialog.Show("선형 패밀리 배치", $"배치 {result.PlacedCount}개\n경고 {result.Warnings.Count}건");
        return Result.Succeeded;
    }
}