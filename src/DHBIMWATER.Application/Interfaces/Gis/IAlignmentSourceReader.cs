using DHBIMWATER.Application.DTOs.Gis;

namespace DHBIMWATER.Application.Interfaces.Gis;

/// <summary>파일 확장자별 선형 소스 리더(SHP, DWG ...)의 공통 인터페이스. UseCase는 구체 포맷 타입을 몰라도 된다.</summary>
public interface IAlignmentSourceReader
{
    bool CanRead(string filePath);
    ShapefileReadResult Read(string filePath);
}
