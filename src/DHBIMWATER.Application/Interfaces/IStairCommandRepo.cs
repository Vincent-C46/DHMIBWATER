using DHBIMWATER.Core.Structures;

namespace DHBIMWATER.Application.Interfaces
{
    public interface IStairCommandRepo
    {
        int CreateStair(StairsDefinition stairsDefinition);
    }
}
