using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.UseCases;
using DHBIMWATER.Application.UseCases.Sheets;
using DHBIMWATER.Infrastructure.Services.Revit.Sheets;
using DHBIMWATER.UI.ViewModels.Documentation;
using DHBIMWATER.UI.Views.Documentation;
using DHBIMWATER.UI.Views.Documentation.Sheets;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Infrastructure.Services.Revit;
using System.Windows.Interop;
using DHBIMWATER.Infrastructure.Services.Revit.Parameter;


namespace DHBIMWATER.Revit.Commands.Sheets
{
    [Transaction(TransactionMode.Manual)]
    public class SheetManagerCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uidoc = commandData.Application.ActiveUIDocument;
            var doc = uidoc.Document;

            var gateway = new SheetGateway(doc, uidoc);
            var useCase = new SheetUseCase(gateway);
            var waterReservoirUseCase = new WaterReservoirUseCase(useCase);
            var pumpingStationUseCase = new PumpingStationUseCase(useCase);

            IDialogService dialogService = new RevitDialogService();

            var vm = new SheetManagerViewModel(useCase, waterReservoirUseCase, pumpingStationUseCase, dialogService);

            var view = new SheetManagerView { DataContext = vm };
            new WindowInteropHelper(view).Owner = commandData.Application.MainWindowHandle;
            view.ShowDialog();

            // 선택 객체 치수 모드는 Revit 선택을 위해 SheetManager를 잠시 닫고, 작업 후 다시 연다.
            if (vm.RequestedCurrentViewSelectedObjects &&
                !string.IsNullOrWhiteSpace(vm.RequestedCurrentViewDimensionTypeName))
            {
                useCase.ApplyDimensionsOnCurrentView(
                    DimensionMode.SelectedObjects,
                    vm.RequestedCurrentViewDimensionTypeName,
                    vm.RequestedCurrentViewDimensionSides,
                    vm.RequestedCurrentViewIncludeOverall);

                var reopenedVm = new SheetManagerViewModel(useCase, waterReservoirUseCase, pumpingStationUseCase, dialogService);
                var reopenedView = new SheetManagerView { DataContext = reopenedVm };
                new WindowInteropHelper(reopenedView).Owner = commandData.Application.MainWindowHandle;
                reopenedView.ShowDialog();
            }

            if (vm.RequestedCurrentViewSelectedAnnotates)
            {
                useCase.ApplyTagsToSelectedOnCurrentView(vm.RequestedAnnotateTagFamilyIds);

                var reopenedVm = new SheetManagerViewModel(useCase, waterReservoirUseCase, pumpingStationUseCase, dialogService);
                var reopenedView = new SheetManagerView { DataContext = reopenedVm };
                new WindowInteropHelper(reopenedView).Owner = commandData.Application.MainWindowHandle;
                reopenedView.ShowDialog();
            }

            if (vm.RequestedCurrentViewAllAnnotates)
            {
                useCase.ApplyTagsToAllOnCurrentView(vm.RequestedAnnotateTagFamilyIds);

                var reopenedVm = new SheetManagerViewModel(useCase, waterReservoirUseCase, pumpingStationUseCase, dialogService);
                var reopenedView = new SheetManagerView { DataContext = reopenedVm };
                new WindowInteropHelper(reopenedView).Owner = commandData.Application.MainWindowHandle;
                reopenedView.ShowDialog();
            }

            return Result.Succeeded;
        }
    }
}
