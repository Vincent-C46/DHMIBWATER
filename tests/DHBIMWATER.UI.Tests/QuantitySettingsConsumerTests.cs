using DHBIMWATER.Application.Services;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Core.Quantity.RuleSets;
using DHBIMWATER.Core.Settings;
using Xunit;

namespace DHBIMWATER.UI.Tests;

public class QuantitySettingsConsumerTests
{
    [Fact]
    public void Create_WithExteriorWallSetting_UsesConfiguredFormworkSpecification()
    {
        var settings = new FormworkSettings
        {
            Walls = new WallFormworkSettings { Exterior = FormworkType.GangForm }
        };

        var rule = DefaultRuleSet.Create(settings).Rules.Single(r =>
            r.WorkType == "거푸집" &&
            r.Formula == "A_right_net" &&
            r.CategoryIds.Contains((int)RevitCategory.Walls) &&
            r.Filters.Any(f => f.ParameterName == "DH_IsExterior" && f.Value == "1"));

        Assert.Equal(FormworkType.GangForm.ToSpecification(), rule.Specification);
    }

    [Fact]
    public void Apply_PreservesMeasurementCategoryId()
    {
        var measurements = new ElementMeasurements
        {
            ElementId = 1,
            CategoryId = (int)RevitCategory.Walls,
            Values = new Dictionary<string, double> { ["Vol"] = 2 },
            Parameters = new Dictionary<string, string> { ["ConcWorkType"] = "철근콘크리트" }
        };
        var rule = new QuantityRule
        {
            WorkType = "철근콘크리트",
            Formula = "Vol",
            Unit = "m³",
            CategoryIds = [(int)RevitCategory.Walls],
            Filters = [new RuleFilter { ParameterName = "ConcWorkType", Value = "철근콘크리트" }]
        };

        var item = new QuantityRuleEngine().Apply(measurements, [rule]).Single();

        Assert.Equal((int)RevitCategory.Walls, item.CategoryId);
    }

    [Fact]
    public void Create_RebarApproximation_UsesConfiguredRatio()
    {
        var concreteItem = new QuantityItem
        {
            ElementId = 1,
            CategoryId = (int)RevitCategory.Walls,
            Category = "벽",
            ElementCode = "W1",
            WorkType = "철근콘크리트",
            Unit = "m³",
            Value = 2
        };

        var result = RebarApproximationCalculator.Create([concreteItem], new RebarRatioSettings()).Single();

        Assert.Equal("철근(개략)", result.WorkType);
        Assert.Equal(0.22, result.Value, 8);
        Assert.Equal("ton", result.Unit);
    }

    [Fact]
    public void CalculateLossValue_WhenDisabled_ReturnsNetValue()
    {
        var result = LossRateCalculator.Calculate(100, "거푸집", new LossRateSettings { Enabled = false });

        Assert.Equal(100, result);
    }

    [Fact]
    public void CalculateLossValue_WhenRateExists_AppliesRate()
    {
        var result = LossRateCalculator.Calculate(100, "거푸집", new LossRateSettings());

        Assert.Equal(105, result);
    }
}
