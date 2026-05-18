namespace DHBIMWATER.Application.DTOs.Revit.PumpingStation
{
    public class PumpExcelDto
    {
        public double PumpDiameter { get; init; }
        public double TotalHead { get; init; }
        public bool IsRectangularOpeningShape { get; init; }
        public double B5 { get; init; }
        public double B6 { get; init; }
        public double SupportBlockB { get; init; }
        public double SupportBlockH { get; init; }
    }
}
