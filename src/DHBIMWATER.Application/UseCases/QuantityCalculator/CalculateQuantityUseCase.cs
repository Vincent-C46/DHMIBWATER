using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Application.Interfaces.Storage;
using DHBIMWATER.Application.Services;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Core.Quantity.RuleSets;
using DHBIMWATER.Core.Settings;

namespace DHBIMWATER.Application.UseCases.QuantityCalculator
{
    public class CalculateQuantityUseCase
    {
        #region Fields
        private readonly ITransactionContext _tx;
        private readonly IDialogService _dialogService;
        private readonly IElementQuantityRepo _elementQuantityRepo;
        private readonly IManualQuantityRepo _manualQuantityRepo;
        private readonly IEnumerable<IQuantityExtractor> _extractors;
        private readonly IEnumerable<IElementMeasurementExtractor> _measurementExtractors;
        private readonly QuantityRuleEngine _ruleEngine;
        private readonly IQuantityRuleRepository _ruleRepo;
        private readonly IQuantitySettingsRepository _settingsRepo;
        #endregion

        #region Constructor
        public CalculateQuantityUseCase(ITransactionContext tx,
                                        IDialogService dialogService,
                                        IElementQuantityRepo elementQuantityRepo,
                                        IManualQuantityRepo manualQuantityRepo,
                                        IEnumerable<IQuantityExtractor> extractors,
                                        IEnumerable<IElementMeasurementExtractor> measurementExtractors,
                                        QuantityRuleEngine ruleEngine,
                                        IQuantityRuleRepository ruleRepo,
                                        IQuantitySettingsRepository settingsRepo)
        {
            _tx = tx;
            _dialogService = dialogService;
            _elementQuantityRepo = elementQuantityRepo;
            _manualQuantityRepo = manualQuantityRepo;
            _extractors = extractors;
            _measurementExtractors = measurementExtractors;
            _ruleEngine = ruleEngine;
            _ruleRepo = ruleRepo;
            _settingsRepo = settingsRepo;
        }
        #endregion

        #region Methods
        public IEnumerable<QuantityItem> Execute()
        {
            var quantityItems = new List<QuantityItem>();
            var manualItems = _manualQuantityRepo.LoadAll();

            // 저장된 수량산출 설정 로드 (없으면 기본값). 거푸집·철근비에 반영.
            var settings = _settingsRepo.Load() ?? new ProjectSettings();

            // 기존 IQuantityExtractor 경로 (벽체 제외 카테고리)
            foreach (var extractor in _extractors)
            {
                var ids = extractor.CollectElementIds();
                if (!ids.Any()) continue;

                foreach (var id in ids)
                    quantityItems.AddRange(extractor.Extract(id));
            }

            // Rule Engine 경로 (DefaultRuleSet 항상 적용 + 프로젝트 특화 규칙 추가)
            // 거푸집 종류는 설정창(FormworkSettings)에서 주입.
            var rules = DefaultRuleSet.Create(settings.Formwork).Rules
                .Concat(_ruleRepo.GetProjectRuleSet()?.Rules ?? [])
                .ToList();
            foreach (var measExtractor in _measurementExtractors)
            {
                var ids = measExtractor.CollectElementIds();
                if (!ids.Any()) continue;

                foreach (var id in ids)
                {
                    var measurements = measExtractor.Extract(id);
                    quantityItems.AddRange(_ruleEngine.Apply(measurements, rules));
                }
            }

            // 철근(개략) 병행 산출 — RC 콘크리트 체적 × 카테고리별 철근비.
            quantityItems.AddRange(
                RebarApproximationCalculator.Create(quantityItems.ToList(), settings.RebarRatio));

            using (_tx)
            {
                try
                {
                    _tx.Begin("Save Quantity");
                    foreach (var group in quantityItems.GroupBy(q => q.ElementId))
                        _elementQuantityRepo.Save(group.Key, group.ToList());
                    _tx.Commit();

                    return quantityItems.Concat(manualItems).ToList();
                }
                catch (Exception ex)
                {
                    _tx.Rollback();
                    _dialogService.Warn("Error", $"Error Message: {ex.Message}");
                    return quantityItems.Concat(manualItems).ToList();
                }
            }
        }
        #endregion
    }
}
