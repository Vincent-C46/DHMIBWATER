using DHBIMWATER.Application.Gis;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Core.Gis;
using Xunit;

namespace DHBIMWATER.UI.Tests.Gis;

public class BendSettingsStorageTests
{
    [Theory]
    [InlineData(true, BendSettingsSource.Project)]
    [InlineData(false, BendSettingsSource.BuiltInDefault)]
    public void Provider_uses_project_then_built_in_default(bool hasProject, BendSettingsSource expected)
    {
        var project = Settings("프로젝트");
        var provider = new BendSettingsProvider(new StubRepo(hasProject ? project : null));

        var (settings, source) = provider.Load();

        Assert.Equal(expected, source);
        Assert.Equal(expected switch
        {
            BendSettingsSource.Project => project.ActiveJointType,
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
}
