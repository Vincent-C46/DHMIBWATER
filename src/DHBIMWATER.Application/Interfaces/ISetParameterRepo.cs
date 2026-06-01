using DHBIMWATER.Application.DTOs.Revit.PumpingStation;

namespace DHBIMWATER.Application.Interfaces
{
    public interface ISetParameterRepo
    {
        void SetTypeParameter (PumpCreationRequestDto dto);
    }
}
