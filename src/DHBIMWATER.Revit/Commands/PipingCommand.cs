using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.UseCases.Gis;
using DHBIMWATER.Revit.DependencyInjection;
using DHBIMWATER.UI.ViewModels.Modeling;
using DHBIMWATER.UI.Views.Modeling;
using System.Windows.Interop;

namespace DHBIMWATER.Revit.Commands
{
    /// <summary>
    /// 관로 모델링 커맨드.
    /// SHP 파일을 해석해 DirectShape 관로 선형을 생성한다.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class PipingCommand : CommandBase
    {
        protected override Result ExecuteInternal(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var view = ServiceContainer.GetService<PipingView>();
            var viewModel = (PipingViewModel)view.DataContext;
            new WindowInteropHelper(view).Owner = commandData.Application.MainWindowHandle;
            view.ShowDialog();
            if (viewModel.RequestedImport is not { } request) return Result.Cancelled;
            var result = ServiceContainer.GetService<ImportPipeAlignmentUseCase>().Execute(request);
            TaskDialog.Show("관로 선형 생성", $"선형 {result.CreatedCount}개 생성\n스킵된 0길이 구간 {result.SkippedSegments}개\n경고 {result.Warnings.Count}건");
            return Result.Succeeded;
        }
    }
}
