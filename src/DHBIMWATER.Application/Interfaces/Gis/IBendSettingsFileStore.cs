using DHBIMWATER.Core.Gis;

namespace DHBIMWATER.Application.Interfaces.Gis;

/// <summary>사용자가 선택한 파일로 관·곡관 설정 전체를 내보내고 불러온다.</summary>
public interface IBendSettingsFileStore
{
    /// <summary>지정한 파일의 설정을 읽는다. 파일이 없거나 형식이 잘못되면 예외를 발생시킨다.</summary>
    BendSettings Load(string path);

    /// <summary>현재 설정 전체를 지정한 파일에 기록한다.</summary>
    void Save(string path, BendSettings settings);
}
