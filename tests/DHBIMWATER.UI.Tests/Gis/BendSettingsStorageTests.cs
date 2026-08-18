using DHBIMWATER.Application.Gis;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Core.Gis;
using DHBIMWATER.Infrastructure.Repositories.Local;
using Xunit;

namespace DHBIMWATER.UI.Tests.Gis;

public class BendSettingsStorageTests
{
    /// <summary>표 객체가 아니라 항목 목록만 직렬화하므로 왕복이 실제로 되는지 확인한다.</summary>
    [Fact]
    public void MasterStore_roundtrips_all_tables()
    {
        var path = Path.Combine(Path.GetTempPath(), $"dhbimwater-master-{Guid.NewGuid():N}.json");
        try
        {
            var store = new JsonFileBendSettingsMasterStore(path);
            Assert.Null(store.Load());

            var settings = new BendSettings(
                new StraightPipeSpecTable(new[] { new StraightPipeSpec(PipeKindCatalog.Water1, 100, 118, 7.5) }),
                new JointDeflectionTable(new[] { new JointDeflectionSpec(JointTypeCatalog.Tyton, 100, 5) }),
                new BendFittingCatalog(new[] { new BendFittingEntry(100, 45, BendForm.BType, 120, 210, 10, 8, "45도") }),
                JointTypeCatalog.Tyton,
                JointApplicationMode.BothJoints);
            store.Save(settings);

            var loaded = store.Load();
            Assert.NotNull(loaded);
            Assert.Equal(7.5, loaded!.StraightPipes.Find(PipeKindCatalog.Water1, 100)!.ThicknessMm);
            Assert.Equal(5, loaded.JointDeflections.AllowableFor(JointTypeCatalog.Tyton, 100));
            Assert.Equal("45도", loaded.Fittings.Find(100, 45, BendForm.BType)!.TypeName);
            Assert.Equal(JointTypeCatalog.Tyton, loaded.ActiveJointType);
            Assert.Equal(JointApplicationMode.BothJoints, loaded.ApplicationMode);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    /// <summary>손상된 마스터 파일 때문에 기능 전체가 죽으면 안 된다.</summary>
    [Fact]
    public void MasterStore_treats_corrupt_file_as_absent()
    {
        var path = Path.Combine(Path.GetTempPath(), $"dhbimwater-master-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, "{ not json");
            Assert.Null(new JsonFileBendSettingsMasterStore(path).Load());
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Theory]
    [InlineData(true, true, BendSettingsSource.Project)]
    [InlineData(false, true, BendSettingsSource.Master)]
    [InlineData(false, false, BendSettingsSource.BuiltInDefault)]
    public void Provider_falls_back_project_then_master_then_default(bool hasProject, bool hasMaster, BendSettingsSource expected)
    {
        var project = Settings("프로젝트");
        var master = Settings("마스터");
        var provider = new BendSettingsProvider(
            new StubRepo(hasProject ? project : null),
            new StubMaster(hasMaster ? master : null));

        var (settings, source) = provider.Load();

        Assert.Equal(expected, source);
        Assert.Equal(expected switch
        {
            BendSettingsSource.Project => project.ActiveJointType,
            BendSettingsSource.Master => master.ActiveJointType,
            _ => BendSettings.Default.ActiveJointType
        }, settings.ActiveJointType);
    }

    private static BendSettings Settings(string jointType) => BendSettings.Default with { ActiveJointType = jointType };

    private sealed class StubRepo : IBendSettingsRepo
    {
        private readonly BendSettings? _stored;
        public StubRepo(BendSettings? stored) => _stored = stored;
        public BendSettings? Load() => _stored;
        public void Save(BendSettings settings) => throw new NotSupportedException();
    }

    private sealed class StubMaster : IBendSettingsMasterStore
    {
        private readonly BendSettings? _stored;
        public StubMaster(BendSettings? stored) => _stored = stored;
        public BendSettings? Load() => _stored;
        public void Save(BendSettings settings) => throw new NotSupportedException();
    }
}
