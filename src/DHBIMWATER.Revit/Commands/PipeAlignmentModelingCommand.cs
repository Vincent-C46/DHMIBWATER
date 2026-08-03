using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Application.UseCases.Gis;
using DHBIMWATER.Revit.DependencyInjection;
using DHBIMWATER.UI.ViewModels.Modeling;
using DHBIMWATER.UI.Views.Modeling;
using System.Windows.Interop;

namespace DHBIMWATER.Revit.Commands;

[Transaction(TransactionMode.Manual)]
public class PipeAlignmentModelingCommand : CommandBase
{
    protected override Result ExecuteInternal(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var view = ServiceContainer.GetService<PipeAlignmentModelingView>();
        var viewModel = (PipeAlignmentModelingViewModel)view.DataContext;
        new WindowInteropHelper(view).Owner = commandData.Application.MainWindowHandle;
        view.ShowDialog();
        if (viewModel.RequestedModeling is not { } request) return Result.Cancelled;
        var result = ServiceContainer.GetService<ModelPipeAlignmentUseCase>().Execute(request);
        var text = result.OutputMode == PipeAlignmentOutputMode.DirectShape
            ? $"선형 {result.CreatedCount}개 생성\n스킵된 0길이 구간 {result.SkippedSegments}개\n경고 {result.Warnings.Count}건"
            : $"배치 {result.CreatedCount}개\n경고 {result.Warnings.Count}건";
        TaskDialog.Show("관로 모델링", text);
        return Result.Succeeded;
    }
}
