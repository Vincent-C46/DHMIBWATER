namespace DHBIMWATER.Application.DTOs.Revit.Sheet
{
    public class SheetInfoDto
    {
        public string Id { get; set; }
        public string SheetNumber { get; set; }
        public string SheetName { get; set; }
        public string ViewDirName { get; set; } // 지금은 보류
        public List<SheetViewDto> Views { get; set; } = new();

    }
}
