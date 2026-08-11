using DHBIMWATER.Core.Structures;

namespace DHBIMWATER.Application.Interfaces
{
    public interface IElementTypeCommandRepo
    {
        int FindOrCreateConcreteMaterial(ConcreteSpec concrete);
        int FindOrCreateSlabType(FloorTypeSpec spec, int materialId);
        int FindOrCreateWallType(WallTypeSpec spec, int materialId);
        int FindOrCreateBeamType(BeamTypeSpec spec);
        int FindBeamSymbol(string typeName);
        int FindHaunchBeamSymbol();
        int FindColumnSymbol(string typeName);
        int FindGenericModelSymbol(string symbolName);
    }
}
