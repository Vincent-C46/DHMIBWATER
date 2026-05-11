using System;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.UseCases.Parameter;
using DHBIMWATER.Revit.DependencyInjection;
using DHBIMWATER.UI.Views.Utilities;
using Microsoft.Win32;

namespace DHBIMWATER.Revit.Commands.Parameter
{
    [Transaction(TransactionMode.Manual)]
    public class ExParamsCommand : CommandBase
    {
        protected override Result ExecuteInternal(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var exportUseCase = ServiceContainer.GetService<IExportParamsUseCase>();
                var importUseCase = ServiceContainer.GetService<IImportParamsUseCase>();

                var view = ServiceContainer.GetService<ExParamsView>();
                var vm = view.DataContext as DHBIMWATER.UI.ViewModels.Utilities.ExParamsViewModel;
                if (vm == null) return Result.Cancelled;

                vm.ImportRequested += path =>
                {
                    try
                    {
                        var doc = commandData.Application.ActiveUIDocument.Document;
                        int modified = importUseCase.Execute(doc, path, overwriteExisting: true);
                        TaskDialog.Show("Import", $"{modified} 개의 파라미터가 수정되었습니다.");
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("Import Error", ex.Message);
                    }
                };

                var result = view.ShowDialog();
                if (result != true) return Result.Cancelled;

                if (vm.SelectedCategory == null || vm.SelectedParameters.Count == 0)
                {
                    TaskDialog.Show("오류", "카테고리와 매개변수를 모두 선택하세요.");
                    return Result.Failed;
                }

                var dialog = new SaveFileDialog
                {
                    Title = "파일 저장",
                    Filter = "Excel 파일 (*.xlsx)|*.xlsx|CSV 파일 (*.csv)|*.csv",
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    FileName = $"Export_{vm.SelectedCategory.DisplayName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                bool? dlgOk = dialog.ShowDialog();
                if (dlgOk != true) return Result.Cancelled;

                string filePath = dialog.FileName;

                exportUseCase.Export(vm.SelectedCategory.Key, vm.SelectedParameters.ToList(), filePath);

                TaskDialog.Show("Export 완료", $"파일이 저장되었습니다:\n{filePath}");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
