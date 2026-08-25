using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Application.Gis;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Gis;
using Xunit;

namespace DHBIMWATER.UI.Tests.Gis;

public sealed class AlignmentSourceLoaderMappingTests
{
    [Fact]
    public void Load_ConfirmedMappingOverridesParsedDiameterAndKind()
    {
        var reader = new StubReader(Feature(new Dictionary<string, string> { ["DIA"] = "상수_D300", ["KIND"] = PipeKindCatalog.Water1 }));
        var file = new AlignmentSourceFile("sample.shp", PipeKindCatalog.Water1, "DIA", "KIND", DiameterMappings:
            new Dictionary<string, ResolvedPipeSpecKey> { ["상수_D300"] = new(450, PipeKindCatalog.Sewer2) });

        var result = new AlignmentSourceLoader(new[] { reader }).Load(new[] { file });

        Assert.Equal(450, result.Alignments[0].DiameterMm);
        Assert.Equal(PipeKindCatalog.Sewer2, result.Alignments[0].PipeKind);
    }

    [Fact]
    public void Load_WithoutMappingPreservesManualDiameterFallback()
    {
        var reader = new StubReader(Feature(new Dictionary<string, string> { ["DIA"] = "직경미상" }));
        var file = new AlignmentSourceFile("sample.shp", PipeKindCatalog.Water2, "DIA", null, ManualDiameterMm: 200);

        var result = new AlignmentSourceLoader(new[] { reader }).Load(new[] { file });

        Assert.Equal(200, result.Alignments[0].DiameterMm);
        Assert.Equal(PipeKindCatalog.Water2, result.Alignments[0].PipeKind);
    }

    [Fact]
    public void Load_MappedKindSurvivesWhenMappedDiameterIsUnspecified()
    {
        var reader = new StubReader(Feature(new Dictionary<string, string> { ["DIA"] = "D300" }));
        var file = new AlignmentSourceFile("sample.shp", PipeKindCatalog.Water1, "DIA", null, DiameterMappings:
            new Dictionary<string, ResolvedPipeSpecKey> { ["D300"] = new(null, PipeKindCatalog.Sewer2) });

        var result = new AlignmentSourceLoader(new[] { reader }).Load(new[] { file });

        Assert.Equal(300, result.Alignments[0].DiameterMm);
        Assert.Equal(PipeKindCatalog.Sewer2, result.Alignments[0].PipeKind);
    }

    [Fact]
    public void Load_EmptyRawValueUsesEmptyValueMappingKey()
    {
        var reader = new StubReader(Feature(new Dictionary<string, string>()));
        var file = new AlignmentSourceFile("sample.shp", string.Empty, null, null, DiameterMappings:
            new Dictionary<string, ResolvedPipeSpecKey> { ["(값 없음)"] = new(300, PipeKindCatalog.Water3) });

        var result = new AlignmentSourceLoader(new[] { reader }).Load(new[] { file });

        Assert.Equal(300, result.Alignments[0].DiameterMm);
        Assert.Equal(PipeKindCatalog.Water3, result.Alignments[0].PipeKind);
    }

    [Fact]
    public void Load_WhenUnknownKindWarningDisabled_PreservesRawKindWithoutWarning()
    {
        var reader = new StubReader(Feature(new Dictionary<string, string> { ["KIND"] = "상수" }));
        var file = new AlignmentSourceFile("sample.shp", string.Empty, null, "KIND");

        var result = new AlignmentSourceLoader(new[] { reader }).Load(new[] { file }, warnOnUnknownPipeKinds: false);

        Assert.Equal("상수", result.Alignments[0].PipeKind);
        Assert.DoesNotContain(result.Warnings, warning => warning.Contains("고정 관종 목록"));
    }

    [Fact]
    public void Load_WhenUnknownKindWarningEnabled_PreservesExistingWarning()
    {
        var reader = new StubReader(Feature(new Dictionary<string, string> { ["KIND"] = "상수" }));
        var file = new AlignmentSourceFile("sample.shp", string.Empty, null, "KIND");

        var result = new AlignmentSourceLoader(new[] { reader }).Load(new[] { file });

        Assert.Contains(result.Warnings, warning => warning.Contains("고정 관종 목록에 없는 값(상수)"));
    }

    private static PipeAlignment Feature(IReadOnlyDictionary<string, string> attributes) => new(
        new[] { new Point3D(0, 0, 0), new Point3D(1, 0, 0) }, string.Empty, 0, "sample.shp", "1", attributes);

    private sealed class StubReader(PipeAlignment feature) : IAlignmentSourceReader
    {
        public bool CanRead(string filePath) => true;
        public ShapefileReadResult Read(string filePath) => new(
            new[] { feature }, Array.Empty<ShapefileFieldInfo>(), ShapefileExtent.Empty, null, null, "UTF-8", 1, 2, 1,
            Array.Empty<string>(), feature.Attributes);
    }
}
