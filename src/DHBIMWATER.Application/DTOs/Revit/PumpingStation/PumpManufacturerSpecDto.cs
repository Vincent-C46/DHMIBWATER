namespace DHBIMWATER.Application.DTOs.Revit.PumpingStation
{
    public record PumpManufacturerSpecDto(
        string OpeningShape,
        double B5,
        double SupportBlockWidth,
        double SupportBlockHeight
    );
}
