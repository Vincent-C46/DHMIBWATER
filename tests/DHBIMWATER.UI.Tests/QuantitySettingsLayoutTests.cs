using System.Text.RegularExpressions;
using Xunit;

namespace DHBIMWATER.UI.Tests;

public class QuantitySettingsLayoutTests
{
    [Fact]
    public void RebarAndLossRateEditors_HaveEnoughRowHeightForThirtyTwoPixelTextBoxes()
    {
        var solutionRoot = FindSolutionRoot();
        var xaml = File.ReadAllText(Path.Combine(
            solutionRoot,
            "src",
            "DHBIMWATER.UI",
            "Views",
            "Quantity",
            "QuantitySettingsView.xaml"));

        foreach (var tabHeader in new[] { "철근비", "할증률" })
        {
            var tabMatch = Regex.Match(
                xaml,
                $"<TabItem Header=\"{tabHeader}\">(?<content>.*?)</TabItem>",
                RegexOptions.Singleline);

            Assert.True(tabMatch.Success, $"'{tabHeader}' 탭을 찾을 수 없습니다.");
            Assert.Matches("<DataGrid[^>]*RowHeight=\"3[8-9]\"", tabMatch.Groups["content"].Value);
        }
    }

    private static string FindSolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "DHBIMWATER.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("DHBIMWATER.sln을 찾을 수 없습니다.");
    }
}
