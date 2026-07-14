using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Application.Interfaces.Storage;
using DHBIMWATER.Application.Services;
using DHBIMWATER.Application.UseCases.QuantityCalculator;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Core.Quantity.RuleSets;
using DHBIMWATER.Core.Settings;
using DHBIMWATER.UI.ViewModels.Quantity;
using Xunit;

namespace DHBIMWATER.UI.Tests.ViewModels.Quantity;

public class QuantityViewModelTests
{
    [Fact]
    public void QuantityViewModel_ExposesQuantityWorkflowCommands()
    {
        var vm = CreateViewModel();

        Assert.NotNull(vm.ExtractCommand);
        Assert.NotNull(vm.SelectInRevitCommand);
        Assert.Null(typeof(QuantityViewModel).GetProperty("OpenSettingsCommand"));
    }

    private static QuantityViewModel CreateViewModel()
    {
        var dialogService = new StubDialogService();
        var calculateQuantityUseCase = new CalculateQuantityUseCase(
            new StubTransactionContext(),
            dialogService,
            new StubElementQuantityRepo(),
            new StubManualQuantityRepo(),
            Array.Empty<IQuantityExtractor>(),
            Array.Empty<IElementMeasurementExtractor>(),
            new QuantityRuleEngine(),
            new StubQuantityRuleRepository(),
            new StubQuantitySettingsRepository());

        return new QuantityViewModel(
            calculateQuantityUseCase,
            new ExportQuantityUseCase(null!, dialogService),
            dialogService,
            new StubFileDialogService());
    }

    private sealed class StubTransactionContext : ITransactionContext
    {
        public void Begin(string name) { }
        public void Commit() { }
        public void Rollback() { }
        public void Dispose() { }
    }

    private sealed class StubDialogService : IDialogService
    {
        public void Info(string title, string message) { }
        public void Warn(string title, string message) { }
        public bool Confirm(string title, string message) => true;
    }

    private sealed class StubFileDialogService : IFileDialogService
    {
        public string? OpenFile(string title, string filter) => null;
        public string? SaveFile(string title, string filter, string defaultFileName = "") => null;
    }

    private sealed class StubElementQuantityRepo : IElementQuantityRepo
    {
        public void Save(long elementId, IEnumerable<QuantityItem> items) { }
        public IReadOnlyList<QuantityItem> Load(long elementId) => Array.Empty<QuantityItem>();
        public void Delete(long elementId) { }
        public bool HasData(long elementId) => false;
    }

    private sealed class StubManualQuantityRepo : IManualQuantityRepo
    {
        public void Save(IEnumerable<QuantityItem> items) { }
        public IReadOnlyList<QuantityItem> LoadAll() => Array.Empty<QuantityItem>();
        public void Clear() { }
    }

    private sealed class StubQuantityRuleRepository : IQuantityRuleRepository
    {
        public RuleSet? GetProjectRuleSet() => null;
        public void SaveProjectRuleSet(RuleSet ruleSet) { }
    }

    private sealed class StubQuantitySettingsRepository : IQuantitySettingsRepository
    {
        public ProjectSettings? Load() => null;
        public void Save(ProjectSettings settings) { }
    }
}
