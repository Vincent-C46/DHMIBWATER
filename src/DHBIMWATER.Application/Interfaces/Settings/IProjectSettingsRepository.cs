using DHBIMWATER.Core.Settings;

namespace DHBIMWATER.Application.Interfaces.Settings
{
    public interface IProjectSettingsRepository
    {
        ProjectSettings? Load(string filePath);
        void Save(ProjectSettings settings, string filePath);
    }
}
