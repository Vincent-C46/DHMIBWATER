namespace DHBIMWATER.Application.DTOs.Revit.Families
{
    public class WebFamilyLoadResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string FamilyName { get; set; } = string.Empty;
        public string SavedPath { get; set; } = string.Empty;
    }
}
