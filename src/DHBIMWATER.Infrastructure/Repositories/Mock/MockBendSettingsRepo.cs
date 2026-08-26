using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Core.Gis;

namespace DHBIMWATER.Infrastructure.Repositories.Mock;

/// <summary>Revit 없이 동작하는 메모리 보관용 구현. Sandbox/테스트 환경에서만 쓴다.</summary>
public sealed class MockBendSettingsRepo : IBendSettingsRepo
{
    private static BendSettings? _stored;

    public BendSettings? Load() => _stored;

    public void Save(BendSettings settings) => _stored = settings;
}
