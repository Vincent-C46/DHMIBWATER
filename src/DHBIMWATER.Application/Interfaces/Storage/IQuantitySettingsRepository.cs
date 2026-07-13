using DHBIMWATER.Core.Settings;

namespace DHBIMWATER.Application.Interfaces.Storage
{
    /// <summary>
    /// 수량산출 설정(ProjectSettings)을 Revit DataStorage에 영구 저장/로드한다.
    /// 파일 기반 입출력은 IProjectSettingsRepository(.dhcfg)를 별도로 사용.
    /// </summary>
    public interface IQuantitySettingsRepository
    {
        /// <summary>저장된 설정을 로드한다. 없으면 null.</summary>
        ProjectSettings? Load();

        /// <summary>설정을 DataStorage에 저장한다. Transaction은 UseCase에서 연다.</summary>
        void Save(ProjectSettings settings);
    }
}
