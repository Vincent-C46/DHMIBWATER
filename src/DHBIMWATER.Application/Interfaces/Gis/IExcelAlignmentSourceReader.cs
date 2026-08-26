using DHBIMWATER.Application.DTOs.Gis;

namespace DHBIMWATER.Application.Interfaces.Gis;

/// <summary>엑셀 소스는 시트·헤더행·열 매핑을 사용자가 지정해야 하므로, 기본 Read(path) 대신 매핑을 받는 오버로드를 추가로 제공한다.</summary>
public interface IExcelAlignmentSourceReader : IAlignmentSourceReader
{
    IReadOnlyList<string> GetSheetNames(string filePath);

    /// <summary>매핑 대화상자 미리보기용. 1행부터 maxRows행까지 셀 값을 문자열로 반환한다.</summary>
    IReadOnlyList<IReadOnlyList<string?>> PreviewRows(string filePath, string sheetName, int maxRows);

    ShapefileReadResult Read(string filePath, ExcelAlignmentMapping mapping);
}
