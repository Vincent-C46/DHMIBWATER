using Autodesk.Revit.UI;
using DHBIMWATER.Application.Interfaces.Storage;
using DHBIMWATER.Application.UseCases.QuantityCalculator;
using DHBIMWATER.Core.Settings;
using System;

namespace DHBIMWATER.Revit.Commands.Quantity
{
    /// <summary>
    /// 수량산출 설정 로드/저장을 Revit API 컨텍스트에서 처리하는 ExternalEvent 핸들러.
    /// Load는 DataStorage 읽기(트랜잭션 불필요), Save는 SaveQuantitySettingsUseCase(트랜잭션)로 위임.
    /// </summary>
    public class QuantitySettingsRequestHandler : IExternalEventHandler
    {
        private readonly IQuantitySettingsRepository _repo;
        private readonly Func<SaveQuantitySettingsUseCase> _saveUseCaseFactory;

        public QuantitySettingsRequest Request { get; } = new();

        /// <summary>Save 요청 시 저장할 설정.</summary>
        public ProjectSettings? PendingSettings { get; set; }

        /// <summary>Open 요청 시 로드된 설정을 UI 스레드에서 넘겨받아 창을 여는 콜백.</summary>
        public Action<ProjectSettings>? OnLoaded { get; set; }

        public QuantitySettingsRequestHandler(
            IQuantitySettingsRepository repo,
            Func<SaveQuantitySettingsUseCase> saveUseCaseFactory)
        {
            _repo = repo;
            _saveUseCaseFactory = saveUseCaseFactory;
        }

        public void Execute(UIApplication uiapp)
        {
            try
            {
                switch (Request.Take())
                {
                    case QuantitySettingsRequestId.Open:
                        var settings = _repo.Load() ?? new ProjectSettings();
                        System.Windows.Application.Current.Dispatcher.Invoke(() => OnLoaded?.Invoke(settings));
                        break;

                    case QuantitySettingsRequestId.Save:
                        if (PendingSettings != null)
                        {
                            _saveUseCaseFactory().Execute(PendingSettings);
                            PendingSettings = null;
                        }
                        break;

                    case QuantitySettingsRequestId.None:
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"수량산출 설정 처리 실패\n{ex.Message}");
            }
        }

        public string GetName() => "QuantitySettingsRequest";
    }
}
