using DHBIMWATER.Core.Structures;

namespace DHBIMWATER.Application.Interfaces
{
    public interface IGenericModelCommandRepo
    {
        int PlaceInstance(GenericModelPlacementDefinition def, long levelId, int symbolId);
    }
}
