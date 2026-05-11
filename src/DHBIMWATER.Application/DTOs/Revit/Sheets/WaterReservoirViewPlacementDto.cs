namespace DHBIMWATER.Application.DTOs.Revit.Sheet
{
    public class WaterReservoirViewPlacementDto
    {
        public string SheetNumber { get; set; }
        public string ViewName { get; set; }
        public int Scale { get; set; }
        public string Form { get; set; }
        public string TitleBlockName { get; set; }
        public string VisualStyle { get; set; }
        public string ViewTitleOnSheet { get; set; }
        public double TitleOffsetX { get; set; }
        public double TitleOffsetY { get; set; }
        public double TitleLineLength { get; set; }
    }
}
