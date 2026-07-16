namespace DHBIMWATER.Application.Interfaces;

/// <summary>활성 Revit 프로젝트의 공유좌표 및 도북각을 설정한다.</summary>
public interface IProjectLocationCommandRepo
{
    void SetInternalOriginSharedPosition(
        double eastWestMeters,
        double northSouthMeters,
        double elevationMeters,
        double trueNorthToProjectNorthClockwiseDegrees);
}
