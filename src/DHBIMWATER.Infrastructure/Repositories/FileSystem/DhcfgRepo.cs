using System.Text.Json;
using System.Text.Json.Serialization;
using DHBIMWATER.Application.Interfaces.Settings;
using DHBIMWATER.Core.Settings;

namespace DHBIMWATER.Infrastructure.Repositories.FileSystem
{
    public class DhcfgRepo : IProjectSettingsRepository
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
            PropertyNameCaseInsensitive = true
        };

        public ProjectSettings? Load(string filePath)
        {
            var json = System.IO.File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<ProjectSettings>(json, _options);
        }

        public void Save(ProjectSettings settings, string filePath)
        {
            var json = JsonSerializer.Serialize(settings, _options);
            System.IO.File.WriteAllText(filePath, json);
        }
    }
}
