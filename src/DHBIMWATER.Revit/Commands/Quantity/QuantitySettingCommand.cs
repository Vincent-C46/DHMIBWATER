using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.Interfaces.Settings;
using DHBIMWATER.Application.Interfaces.Storage;
using DHBIMWATER.Application.UseCases.QuantityCalculator;
using DHBIMWATER.Revit.DependencyInjection;
using DHBIMWATER.UI.ViewModels.Quantity;
using DHBIMWATER.UI.Views.Quantity;
using System.Windows.Interop;

namespace DHBIMWATER.Revit.Commands.Quantity
{
    [Transaction(TransactionMode.Manual)]
    public class QuantitySettingCommand : CommandBase
    {
        protected override Result ExecuteInternal(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var settingsRepo = ServiceContainer.GetService<IQuantitySettingsRepository>();
            // 모델리스 창이라 커맨드 반환 후 CommandBase.finally에서 ServiceContainer가 Dispose된다.
            // 저장 시점에 지연 resolve하면 "ServiceContainer is not built" 예외가 나므로,
            // 다른 서비스처럼 미리 resolve해 캡처한다. (SaveUseCase의 트랜잭션 컨텍스트는
            // Execute 후 내부 Transaction이 null로 초기화되어 재저장에도 재사용 안전)
            var saveUseCase = ServiceContainer.GetService<SaveQuantitySettingsUseCase>();
            var settingsHandler = new QuantitySettingsRequestHandler(
                settingsRepo,
                () => saveUseCase);
            var settingsEvent = ExternalEvent.Create(settingsHandler);

            var fileDialog = ServiceContainer.GetService<IFileDialogService>();
            var dhcfgRepo = ServiceContainer.GetService<IProjectSettingsRepository>();

            // 로드 완료 → UI 스레드에서 설정창 오픈
            settingsHandler.OnLoaded = settings =>
            {
                var vm = new QuantitySettingsViewModel(settings);
                var view = new QuantitySettingsView(vm);
                new WindowInteropHelper(view).Owner = commandData.Application.MainWindowHandle;
                WireSettingsVm(vm, view, settingsHandler, settingsEvent, fileDialog, dhcfgRepo);
                view.Show();
            };

            // 리본 버튼 → 로드 요청
            settingsHandler.Request.Make(QuantitySettingsRequestId.Open);
            settingsEvent.Raise();
            return Result.Succeeded;
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
    }
}
