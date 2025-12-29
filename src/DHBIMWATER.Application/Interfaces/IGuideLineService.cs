using DHBIMWATER.Application.DTOs.GuildeLine;

namespace DHBIMWATER.Application.Interfaces
{
    public interface IGuideLineService
    {
        List<RevitFamilyDto> GetFamilies();
        List<RevitColumnDto> GetColumn();
    }
}
