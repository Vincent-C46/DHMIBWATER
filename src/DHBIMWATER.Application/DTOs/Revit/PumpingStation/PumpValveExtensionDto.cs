namespace DHBIMWATER.Application.DTOs.Revit.PumpingStation
{
    public record PumpValveExtensionDto(
        double TotalExtension,
        double ValveExtension,
        // 역지밸브 없음(HasCheckValve=false) → G,H,I,J열
        PumpValveDimensionDto WithoutCheckValve,
        // 역지밸브 있음(HasCheckValve=true) → N,O,P,Q열
        PumpValveDimensionDto WithCheckValve);

    // 밸브받침 제원 (G/H/I/J = N/O/P/Q 동일 구조)
    public record PumpValveDimensionDto(
        double ValveBaseWidth,      // 밸브받침폭 (G/N열)
        double ValveBaseLength,     // (H/O열)
        double ValveBaseHeight,     // (I/P열)
        double ValveBasePlacement); // (J/Q열)
}
