using DHBIMWATER.Application.DTOs.Revit.Families;

namespace DHBIMWATER.Application.Interfaces.Families
{
    public interface IWebFamilyLibraryService
    {
        IList<WebFamilyLibraryItemDto> GetFamilies(string apiUrl);
        WebFamilyLoadResultDto LoadFamily(string downloadUrl);
    }
}
