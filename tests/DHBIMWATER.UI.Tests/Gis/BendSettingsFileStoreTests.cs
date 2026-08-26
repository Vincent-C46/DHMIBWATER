using System.IO;
using DHBIMWATER.Core.Gis;
using DHBIMWATER.Infrastructure.Repositories.Local;
using Xunit;

namespace DHBIMWATER.UI.Tests.Gis;

public sealed class BendSettingsFileStoreTests
{
    [Fact]
    public void FileStore_roundtrips_all_settings()
    {
        var path = TempPath();
        try
        {
            var store = new JsonFileBendSettingsFileStore();
            var settings = new BendSettings(
                new StraightPipeSpecTable(new[] { new StraightPipeSpec(PipeKindCatalog.Water1, 100, 118, 7.5) }),
                new JointDeflectionTable(new[] { new JointDeflectionSpec(JointTypeCatalog.Tyton, 100, 5) }),
                new BendFittingCatalog(new[] { new BendFittingEntry(100, 45, BendForm.BType, 120, 210, WallThicknessMm: 8) }),
                JointTypeCatalog.Tyton,
                JointApplicationMode.BothJoints,
                BendConnection.Flanged);

            store.Save(path, settings);
            var loaded = store.Load(path);

            Assert.Equal(7.5, loaded.StraightPipes.Find(PipeKindCatalog.Water1, 100)!.ThicknessMm);
            Assert.Equal(8, loaded.Fittings.Find(100, 45)!.WallThicknessMm);
            Assert.Equal(5, loaded.JointDeflections.AllowableFor(JointTypeCatalog.Tyton, 100));
            Assert.Equal(JointTypeCatalog.Tyton, loaded.ActiveJointType);
            Assert.Equal(JointApplicationMode.BothJoints, loaded.ApplicationMode);
            Assert.Equal(BendConnection.Flanged, loaded.ActiveBendConnection);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void FileStore_rejects_corrupt_json()
    {
        var path = TempPath();
        try
        {
            File.WriteAllText(path, "{ not json");
            Assert.Throws<InvalidDataException>(() => new JsonFileBendSettingsFileStore().Load(path));
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void FileStore_rejects_json_without_required_tables()
    {
        var path = TempPath();
        try
        {
            File.WriteAllText(path, "{}");
            Assert.Throws<InvalidDataException>(() => new JsonFileBendSettingsFileStore().Load(path));
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static string TempPath() => Path.Combine(Path.GetTempPath(), $"dhbimwater-pipe-settings-{Guid.NewGuid():N}.json");
}
