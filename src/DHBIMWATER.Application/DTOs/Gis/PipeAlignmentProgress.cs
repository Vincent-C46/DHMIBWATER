namespace DHBIMWATER.Application.DTOs.Gis;

public enum PipeAlignmentPhase
{
    Loading,
    PlanningBends,
    PlacingStraights,
    PlacingBends,
    Committing
}

public sealed record PipeAlignmentProgress(PipeAlignmentPhase Phase, int Completed, int Total)
{
    public double Percent => Total <= 0 ? 0 : Math.Min(100.0, Completed * 100.0 / Total);
}
