using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Core.Gis;

namespace DHBIMWATER.Infrastructure.Repositories.Local;

/// <summary>
/// 마스터 규격을 <c>%APPDATA%\DHBIMWATER\bend-settings.json</c>에 저장한다.
/// Revit 문서 밖이라 트랜잭션이 없고, 어떤 문서를 열어도 같은 값을 읽는다.
/// </summary>
public sealed class JsonFileBendSettingsMasterStore : IBendSettingsMasterStore
{
    private const string FolderName = "DHBIMWATER";
    private const string FileName = "bend-settings.json";

    /// <summary>BendForm·PipeMaterial을 이름으로 남겨 열거형 순서가 바뀌어도 견디게 한다.</summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _path;

    public JsonFileBendSettingsMasterStore()
        : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), FolderName, FileName)) { }

    /// <summary>테스트에서 임시 경로를 주입하기 위한 생성자.</summary>
    public JsonFileBendSettingsMasterStore(string path) => _path = path;

    public BendSettings? Load()
    {
        // 마스터는 없는 게 정상 상태(첫 실행)이므로 파일 부재를 예외로 다루지 않는다.
        if (!File.Exists(_path)) return null;
        try
        {
            var dto = JsonSerializer.Deserialize<MasterDto>(File.ReadAllText(_path), JsonOptions);
            if (dto is null) return null;
            return new BendSettings(
                new StraightPipeSpecTable(dto.StraightPipes ?? new()),
                new JointDeflectionTable(dto.JointDeflections ?? new()),
                new BendFittingCatalog(dto.Fittings ?? new()),
                string.IsNullOrWhiteSpace(dto.ActiveJointType) ? JointTypeCatalog.KpMechanical : dto.ActiveJointType,
                dto.ApplicationMode);
        }
        catch (Exception ex) when (ex is JsonException or System.IO.IOException or UnauthorizedAccessException)
        {
            // 손상된 마스터 때문에 기능 전체가 죽지 않도록 없는 것으로 본다.
            // 호출부는 프로젝트 값 → 내장 기본값으로 폴백한다.
            return null;
        }
    }

    public void Save(BendSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var dto = new MasterDto
        {
            StraightPipes = settings.StraightPipes.Entries.ToList(),
            JointDeflections = settings.JointDeflections.Entries.ToList(),
            Fittings = settings.Fittings.Entries.ToList(),
            ActiveJointType = settings.ActiveJointType,
            ApplicationMode = settings.ApplicationMode
        };
        File.WriteAllText(_path, JsonSerializer.Serialize(dto, JsonOptions));
    }

    /// <summary>BendSettings는 표 객체를 담고 있어 그대로 직렬화되지 않으므로 항목 목록만 옮긴다.</summary>
    private sealed class MasterDto
    {
        public List<StraightPipeSpec>? StraightPipes { get; set; }
        public List<JointDeflectionSpec>? JointDeflections { get; set; }
        public List<BendFittingEntry>? Fittings { get; set; }
        public string? ActiveJointType { get; set; }
        public JointApplicationMode ApplicationMode { get; set; }
    }
}
