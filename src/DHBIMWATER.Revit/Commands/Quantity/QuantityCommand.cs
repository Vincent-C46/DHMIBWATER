using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.Interfaces.Settings;
using DHBIMWATER.Application.Interfaces.Storage;
using DHBIMWATER.Application.UseCases.QuantityCalculator;
using DHBIMWATER.Core.Settings;
using DHBIMWATER.Revit.DependencyInjection;
using DHBIMWATER.UI.ViewModels.Quantity;
using DHBIMWATER.UI.Views.Quantity;
using System.Windows.Interop;

namespace DHBIMWATER.Revit.Commands.Quantity
{
    [Transaction(TransactionMode.Manual)]
    public class QuantityCommand : CommandBase
    {
        private QuantityView? _view;

        protected override Result ExecuteInternal(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            _view = ServiceContainer.GetService<QuantityView>();

            if (_view != null && _view.IsVisible)
            {
                _view.Activate();
                return Result.Succeeded;
            }

            new WindowInteropHelper(_view).Owner = commandData.Application.MainWindowHandle;
            var useCase = ServiceContainer.GetService<CalculateQuantityUseCase>();
            var handler = new QuantityRequestHandler(useCase, _view.ViewModel);
            var exEvent = ExternalEvent.Create(handler);
            var measureService = new RevitMeasurePickService();

            _view.ViewModel.SetMeasureService(measureService);
            _view.ViewModel.SetExtractAction(() =>
            {
                handler.QuantityRequest.Make(QuantityRequestId.Calculate);
                exEvent.Raise();
            });

            _view.ViewModel.SetSelectAction(ids =>
            {
                handler.ElementIdsToSelect = ids;
                handler.QuantityRequest.Make(QuantityRequestId.SelectInRevit);
                exEvent.Raise();
            });

            WireSettings();

            _view.Show();
            return Result.Succeeded;
        }

        #region 수량산출 설정창 (⚙)
        /// <summary>설정창 열기(로드)·저장(DataStorage)·.dhcfg 입출력을 ExternalEvent로 배선한다.</summary>
        private void WireSettings()
        {
            var settingsRepo = ServiceContainer.GetService<IQuantitySettingsRepository>();
            var settingsHandler = new QuantitySettingsRequestHandler(
                settingsRepo,
                () => ServiceContainer.GetService<SaveQuantitySettingsUseCase>());
            var settingsEvent = ExternalEvent.Create(settingsHandler);

            var fileDialog = ServiceContainer.GetService<IFileDialogService>();
            var dhcfgRepo = ServiceContainer.GetService<IProjectSettingsRepository>();

            // 로드 완료 → UI 스레드에서 설정창 오픈
            settingsHandler.OnLoaded = settings =>
            {
                var vm = new QuantitySettingsViewModel(settings);
                var view = new QuantitySettingsView(vm) { Owner = _view };
                WireSettingsVm(vm, view, settingsHandler, settingsEvent, fileDialog, dhcfgRepo);
                view.Show();
            };

            // ⚙ 버튼 → 로드 요청
            _view!.ViewModel.SetSettingsAction(() =>
            {
                settingsHandler.Request.Make(QuantitySettingsRequestId.Open);
                settingsEvent.Raise();
            });
        }

        /// <summary>설정창 VM의 저장/가져오기/내보내기 이벤트를 배선한다 (가져오기 시 재바인딩에도 재사용).</summary>
        private void WireSettingsVm(
            QuantitySettingsViewModel vm,
            QuantitySettingsView view,
            QuantitySettingsRequestHandler settingsHandler,
            ExternalEvent settingsEvent,
            IFileDialogService fileDialog,
            IProjectSettingsRepository dhcfgRepo)
        {
            const string filter = "DHBIMWATER 설정 (*.dhcfg)|*.dhcfg";

            vm.SaveRequested += s =>
            {
                settingsHandler.PendingSettings = s;
                settingsHandler.Request.Make(QuantitySettingsRequestId.Save);
                settingsEvent.Raise();
            };

            vm.ExportRequested += s =>
            {
                var path = fileDialog.SaveFile("수량산출 설정 내보내기", filter, "quantity_settings.dhcfg");
                if (!string.IsNullOrEmpty(path))
                    dhcfgRepo.Save(s, path);
            };

            vm.ImportRequested += () =>
            {
                var path = fileDialog.OpenFile("수량산출 설정 가져오기", filter);
                if (string.IsNullOrEmpty(path)) return;

                var loaded = dhcfgRepo.Load(path);
                if (loaded is null) return;

                // 새 설정으로 VM 재생성 후 재바인딩
                var newVm = new QuantitySettingsViewModel(loaded);
                WireSettingsVm(newVm, view, settingsHandler, settingsEvent, fileDialog, dhcfgRepo);
                newVm.CloseRequested += _ => view.Close();
                view.DataContext = newVm;
            };
        }
        #endregion
    }
}
