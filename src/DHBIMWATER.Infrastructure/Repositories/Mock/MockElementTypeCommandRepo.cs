using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Structures;

namespace DHBIMWATER.Infrastructure.Repositories.Mock
{
    internal class MockElementTypeCommandRepo : IElementTypeCommandRepo
    {
        public int FindOrCreateSlabType(FloorTypeSpec spec) => 1;
        public int FindOrCreateWallType(WallTypeSpec spec) => 1;
        public int FindOrCreateBeamType(BeamTypeSpec spec) => 1;
    }
}
