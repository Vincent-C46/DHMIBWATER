using DHBIMWATER.Core.Gis;
using Xunit;

namespace DHBIMWATER.UI.Tests.Gis;

public class AlignmentAttributeParserTests
{
    [Theory]
    [InlineData("상수_D100", true, 100, "상수")]
    [InlineData("DCIP_D1350.5", true, 1350.5, "DCIP")]
    [InlineData("_D100", true, 100, null)]
    [InlineData("D100", true, 100, null)]
    [InlineData("100", true, 100, null)]
    [InlineData("100mm", true, 100, null)]
    [InlineData("100 MM", true, 100, null)]
    [InlineData("주철관", false, 0, "주철관")]
    public void ParseDiameter_ParsesSupportedFormats(string raw, bool success, double diameter, string? kind)
    {
        var result = AlignmentAttributeParser.ParseDiameter(raw);
        Assert.Equal(success, result.Success); Assert.Equal(diameter, result.DiameterMm); Assert.Equal(kind, result.Kind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseDiameter_ReturnsUnresolvedForBlank(string? raw)
        => Assert.Equal(new DiameterParseResult(false, 0, null), AlignmentAttributeParser.ParseDiameter(raw));

    [Fact] public void GuessDiameterField_UsesActualFieldOrder() => Assert.Equal("구경", AlignmentAttributeParser.GuessDiameterField(new[] { "구경", "Diameter" }));
    [Fact] public void GuessDiameterField_ReturnsFirstMatchedField() => Assert.Equal("PIP_DIA", AlignmentAttributeParser.GuessDiameterField(new[] { "PIPE_ID", "PIP_DIA", "MTRL" }));
    [Fact] public void GuessDiameterField_ReturnsNullWithoutCandidate() => Assert.Null(AlignmentAttributeParser.GuessDiameterField(new[] { "PIPE_ID", "MTRL" }));
    [Fact] public void GuessKindField_ReturnsFirstMatchedField() => Assert.Equal("MTRL", AlignmentAttributeParser.GuessKindField(new[] { "PIPE_ID", "MTRL" }));
}
