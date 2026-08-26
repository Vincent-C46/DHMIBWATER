using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Application.UseCases.Gis;
using DHBIMWATER.UI.ViewModels.Modeling;
using Xunit;

namespace DHBIMWATER.UI.Tests.ViewModels.Modeling;

public sealed class PipeAlignmentModelingViewModelTests
{
    [Fact]
    public void ApplyProgress_ShowsEtaOnlyAfterMinimumSamplesAndResetsForNewPhase()
    {
        var vm = CreateWithoutServices();

        vm.ApplyProgress(new PipeAlignmentProgress(PipeAlignmentPhase.PlacingStraights, 0, 10));
        Assert.Equal("0 / 10개 · 계산 중…", vm.ProgressDetail);

        vm.ApplyProgress(new PipeAlignmentProgress(PipeAlignmentPhase.PlacingStraights, 5, 10));
        Assert.StartsWith("5 / 10개 · 약 ", vm.ProgressDetail);
        Assert.EndsWith(" 남음", vm.ProgressDetail);

        vm.ApplyProgress(new PipeAlignmentProgress(PipeAlignmentPhase.PlacingStraights, 10, 10));
        Assert.Equal("10 / 10개", vm.ProgressDetail);

        vm.ApplyProgress(new PipeAlignmentProgress(PipeAlignmentPhase.PlacingBends, 0, 4));
        Assert.Equal("0 / 4개 · 계산 중…", vm.ProgressDetail);
    }

    [Fact]
    public void ApplyProgress_HidesDetailForPhaseWithoutTotal()
    {
        var vm = CreateWithoutServices();

        vm.ApplyProgress(new PipeAlignmentProgress(PipeAlignmentPhase.Committing, 0, 0));

        Assert.Equal("모델 저장 중", vm.PhaseText);
        Assert.Empty(vm.ProgressDetail);
    }

    [Theory]
    [InlineData(0, 30, "30초")]
    [InlineData(0, 0, "1초")]
    [InlineData(12, 34, "12분 34초")]
    [InlineData(120, 0, "2시간 0분")]
    public void FormatRemaining_FormatsSecondsMinutesAndHours(int minutes, int seconds, string expected)
    {
        var method = typeof(PipeAlignmentModelingViewModel).GetMethod("FormatRemaining", BindingFlags.NonPublic | BindingFlags.Static)!;

        var actual = (string)method.Invoke(null, new object[] { TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds) })!;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SummarizeSuppressedWarnings_GroupsRepeatedDescriptions()
    {
        var method = typeof(ModelPipeAlignmentUseCase).GetMethod("SummarizeSuppressedWarnings", BindingFlags.NonPublic | BindingFlags.Static)!;

        var actual = (IReadOnlyList<string>)method.Invoke(null, new object[] { new[] { "짧은 요소", "겹침", "짧은 요소" } })!;

        var summary = Assert.Single(actual);
        Assert.Contains("Revit 경고 3건", summary);
        Assert.Contains("짧은 요소 2건", summary);
        Assert.Contains("겹침 1건", summary);
    }

    private static PipeAlignmentModelingViewModel CreateWithoutServices()
    {
        var vm = (PipeAlignmentModelingViewModel)RuntimeHelpers.GetUninitializedObject(typeof(PipeAlignmentModelingViewModel));
        typeof(PipeAlignmentModelingViewModel).GetField("_phaseStopwatch", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(vm, new Stopwatch());
        return vm;
    }
}
