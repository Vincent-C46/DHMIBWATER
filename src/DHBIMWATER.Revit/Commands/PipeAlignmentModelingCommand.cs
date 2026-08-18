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
        var text = result.OutputMode switch
        {
            PipeAlignmentOutputMode.DirectShape => $"선형 {result.CreatedCount}개 생성\n스킵된 0길이 구간 {result.SkippedSegments}개\n경고 {result.Warnings.Count}건",
            PipeAlignmentOutputMode.Adaptive => $"직관 {result.CreatedCount}개\n곡관 {result.BendCount}개\n경고 {result.Warnings.Count}건",
            _ => $"배치 {result.CreatedCount}개\n경고 {result.Warnings.Count}건"
        };
        if (result.Warnings.Count > 0)
            text += $"\n\n{string.Join("\n", result.Warnings.Take(20))}{(result.Warnings.Count > 20 ? $"\n… 외 {result.Warnings.Count - 20}건" : string.Empty)}";
        TaskDialog.Show("관로 모델링", text);
        return Result.Succeeded;
    }
}
