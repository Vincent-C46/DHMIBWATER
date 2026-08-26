using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Application.Gis;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Gis;
using Xunit;

namespace DHBIMWATER.UI.Tests.Gis;

/// <summary>레코드 단위 방향 반전(시작점↔끝점 교환) 규칙.</summary>
public sealed class AlignmentSourceLoaderDirectionTests
{
    [Fact]
    public void Load_ReversedRecordSwapsStartAndEnd()
    {
        var reader = new StubReader(Feature("1"), Feature("2"));
        var file = new AlignmentSourceFile("sample.shp", PipeKindCatalog.Water1, null, null, ReversedRecordNumbers: new[] { "2" });

        var result = new AlignmentSourceLoader(new[] { reader }).Load(new[] { file });

        // 지정하지 않은 레코드는 원본 순서 그대로다.
        Assert.Equal(new Point3D(0, 0, 0), result.Alignments[0].Vertices[0]);
        Assert.Equal(new Point3D(2, 0, 0), result.Alignments[0].Vertices[^1]);
        // 지정한 레코드만 뒤집힌다 — 좌표값 자체는 그대로이고 순서만 바뀐다.
        Assert.Equal(new Point3D(2, 0, 0), result.Alignments[1].Vertices[0]);
        Assert.Equal(new Point3D(1, 0, 0), result.Alignments[1].Vertices[1]);
        Assert.Equal(new Point3D(0, 0, 0), result.Alignments[1].Vertices[^1]);
    }

    [Fact]
    public void Load_WithoutReversalKeepsSourceOrder()
    {
        var reader = new StubReader(Feature("1"));
        var file = new AlignmentSourceFile("sample.shp", PipeKindCatalog.Water1, null, null);

        var result = new AlignmentSourceLoader(new[] { reader }).Load(new[] { file });

        Assert.Equal(new Point3D(0, 0, 0), result.Alignments[0].Vertices[0]);
        Assert.Equal(new Point3D(2, 0, 0), result.Alignments[0].Vertices[^1]);
    }

    private static PipeAlignment Feature(string recordNumber) => new(
        new[] { new Point3D(0, 0, 0), new Point3D(1, 0, 0), new Point3D(2, 0, 0) },
        string.Empty, 0, "sample.shp", recordNumber, new Dictionary<string, string>());

    private sealed class StubReader(params PipeAlignment[] features) : IAlignmentSourceReader
    {
        public bool CanRead(string filePath) => true;
        public ShapefileReadResult Read(string filePath) => new(
            features, Array.Empty<ShapefileFieldInfo>(), ShapefileExtent.Empty, null, null, "UTF-8",
            features.Length, features.Sum(x => x.Vertices.Count), features.Length,
            Array.Empty<string>(), features[0].Attributes);
    }
}
