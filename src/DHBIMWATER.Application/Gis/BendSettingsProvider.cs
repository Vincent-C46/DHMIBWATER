using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Core.Gis;

namespace DHBIMWATER.Application.Gis;

/// <summary>규격 설정이 어디서 왔는지. 사용자에게 안내 문구를 다르게 보여주기 위해 구분한다.</summary>
public enum BendSettingsSource
{
    /// <summary>이 Revit 문서의 DataStorage에 저장된 값.</summary>
    Project,

    /// <summary>앱 레벨 마스터 파일의 값(이 문서에는 아직 저장된 적 없음).</summary>
    Master,

    /// <summary>어디에도 저장된 값이 없어 코드 내장 기본값을 쓴 경우.</summary>
    BuiltInDefault
}

public sealed record BendSettingsResolution(BendSettings Settings, BendSettingsSource Source);

/// <summary>
/// 프로젝트 → 마스터 → 내장 기본값 순으로 규격 설정을 해석한다.
/// 마스터를 두는 이유: 핸드북 제원은 프로젝트 무관 데이터인데 DataStorage에만 두면
/// 새 프로젝트마다 다시 입력해야 한다.
/// </summary>
public sealed class BendSettingsProvider
{
    private readonly IBendSettingsRepo _project;
    private readonly IBendSettingsMasterStore _master;

    public BendSettingsProvider(IBendSettingsRepo project, IBendSettingsMasterStore master)
    { _project = project; _master = master; }

    public BendSettingsResolution Load()
    {
        if (_project.Load() is { } project) return new(project, BendSettingsSource.Project);
        if (_master.Load() is { } master) return new(master, BendSettingsSource.Master);
        return new(BendSettings.Default, BendSettingsSource.BuiltInDefault);
    }
}
