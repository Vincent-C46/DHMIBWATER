using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Structures;

namespace DHBIMWATER.Infrastructure.Repositories.Mock
{
    internal class MockElementTypeCommandRepo : IElementTypeCommandRepo
    {
        public int FindOrCreateConcreteMaterial(ConcreteSpec concrete) => 1;
        public int FindOrCreateSlabType(FloorTypeSpec spec, int materialId) => 1;
        public int FindOrCreateWallType(WallTypeSpec spec, int materialId) => 1;
        public int FindOrCreateBeamType(BeamTypeSpec spec) => 1;
        public int FindBeamSymbol(string typeName) => 1;
        public int FindHaunchBeamSymbol() => 1;
        public int FindColumnSymbol(string typeName) => 1;
        public int FindGenericModelSymbol(string symbolName) => 1;
    }
}
