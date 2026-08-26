using ClosedXML.Excel;
using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Core.Gis;
using DHBIMWATER.Infrastructure.Repositories.Gis;
using Xunit;

namespace DHBIMWATER.UI.Tests.Gis;

/// <summary>엑셀 헤더행이 직경·관종 필드 후보로 노출되는지 실제 xlsx를 만들어 검증한다(docs/44).</summary>
public sealed class ExcelAlignmentReaderFieldTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"dhbim_excel_{Guid.NewGuid():N}.xlsx");

    // 열 배치(1-based): 1=측점, 2=X, 3=Y, 4=Z, 5=직경, 6=관종, 7=직경(중복 헤더)
    public ExcelAlignmentReaderFieldTests()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("관로1");
        string[] header = ["측점", "X", "Y", "Z", "직경", "관종", "직경"];
        for (var c = 0; c < header.Length; c++) sheet.Cell(1, c + 1).Value = header[c];

        (string Station, double X, double Y, double Z, string Diameter, string Kind, string Duplicate)[] rows =
        [
            ("0+000", 100.0, 200.0, 10.0, "D300", "덕타일주철관", "예비1"),
            ("0+020", 120.0, 200.0, 10.5, "D300", "덕타일주철관", "예비2"),
            ("0+040", 140.0, 200.0, 11.0, "D400", "덕타일주철관", "예비3"),
        ];
        for (var i = 0; i < rows.Length; i++)
        {
            var r = i + 2;
            sheet.Cell(r, 1).Value = rows[i].Station;
            sheet.Cell(r, 2).Value = rows[i].X;
            sheet.Cell(r, 3).Value = rows[i].Y;
            sheet.Cell(r, 4).Value = rows[i].Z;
            sheet.Cell(r, 5).Value = rows[i].Diameter;
            sheet.Cell(r, 6).Value = rows[i].Kind;
            sheet.Cell(r, 7).Value = rows[i].Duplicate;
        }
        workbook.SaveAs(_path);
    }

    public void Dispose() { if (File.Exists(_path)) File.Delete(_path); }

    private ShapefileReadResult Read() => new ExcelAlignmentReader().Read(_path,
        new ExcelAlignmentMapping("관로1", HeaderRow: 1, DataStartRow: 2, XColumnIndex: 1, YColumnIndex: 2, ZColumnIndex: 3, StationColumnIndex: 0));

    [Fact]
    public void 헤더행의_열_이름이_필드로_노출되고_XYZ열은_제외된다()
    {
        var names = Read().Fields.Select(x => x.Name).ToList();

        Assert.Contains("직경", names);
        Assert.Contains("관종", names);
        Assert.Contains("측점", names);
        Assert.DoesNotContain("X", names);
        Assert.DoesNotContain("Y", names);
        Assert.DoesNotContain("Z", names);
    }

    [Fact]
    public void 헤더_이름이_중복되면_뒤에_오는_열에_접미사를_붙인다()
    {
        var result = Read();

        Assert.Contains("직경_2", result.Fields.Select(x => x.Name));
        Assert.Equal("예비1", result.Features.Single().Attributes["직경_2"]);
    }

    [Fact]
    public void 열마다_처음_만난_공백아닌_값이_대표값으로_실린다()
    {
        var attributes = Read().Features.Single().Attributes;

        Assert.Equal("D300", attributes["직경"]);
        Assert.Equal("덕타일주철관", attributes["관종"]);
    }

    [Fact]
    public void 값이_섞인_열은_경고하고_측점열은_경고하지_않는다()
    {
        var warnings = Read().Warnings;

        Assert.Contains(warnings, x => x.Contains("'직경' 열에 서로 다른 값이 2종"));
        Assert.DoesNotContain(warnings, x => x.Contains("'측점' 열에"));
        Assert.DoesNotContain(warnings, x => x.Contains("'관종' 열에"));
    }

    [Fact]
    public void 노출된_필드로_직경_관종_열을_자동_추정할_수_있다()
    {
        var names = Read().Fields.Select(x => x.Name).ToList();

        Assert.Equal("직경", AlignmentAttributeParser.GuessDiameterField(names));
        Assert.Equal("관종", AlignmentAttributeParser.GuessKindField(names));
    }
}
