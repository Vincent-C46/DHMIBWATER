using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Core.Gis;

namespace DHBIMWATER.Application.Gis;

/// <summary>규격 설정이 어디서 왔는지. 사용자에게 안내 문구를 다르게 보여주기 위해 구분한다.</summary>
public enum BendSettingsSource
{
    /// <summary>이 Revit 문서의 DataStorage에 저장된 값.</summary>
    Project,

    /// <summary>어디에도 저장된 값이 없어 코드 내장 기본값을 쓴 경우.</summary>
    BuiltInDefault
}

public sealed record BendSettingsResolution(BendSettings Settings, BendSettingsSource Source);

/// <summary>
/// 프로젝트 저장값이 있으면 사용하고, 없으면 코드 내장 기본값을 사용한다.
/// 파일에서 불러온 값은 규격표 화면에서 사용자가 확인한 뒤 프로젝트에 명시적으로 저장한다.
/// </summary>
public sealed class BendSettingsProvider
{
    private readonly IBendSettingsRepo _project;

    public BendSettingsProvider(IBendSettingsRepo project) => _project = project;

    public BendSettingsResolution Load()
    {
        if (_project.Load() is { } project) return new(project, BendSettingsSource.Project);
        return new(BendSettings.Default, BendSettingsSource.BuiltInDefault);
    }
}
