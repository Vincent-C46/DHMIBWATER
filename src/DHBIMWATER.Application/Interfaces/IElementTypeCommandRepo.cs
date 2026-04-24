using DHBIMWATER.Core.Structures;

namespace DHBIMWATER.Application.Interfaces
{
    public interface IElementTypeCommandRepo
    {
        int FindOrCreateSlabType(FloorTypeSpec spec);
        int FindOrCreateWallType(WallTypeSpec spec);
        int FindOrCreateBeamType(BeamTypeSpec spec);
    }
}
