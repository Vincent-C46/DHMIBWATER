namespace DHBIMWATER.Application.Interfaces;

/// <summary>활성 Revit 프로젝트의 공유좌표 및 도북각을 설정한다.</summary>
public interface IProjectLocationCommandRepo
{
    /// <summary>
    /// 프로젝트 기준점(PBP)의 공유좌표가 (eastWestMeters, northSouthMeters, 표고 0)이 되도록 내부 원점의 공유좌표를 설정한다.
    /// 표고 인자가 없는 이유: PBP 표고는 항상 0으로 고정하고 모델 높이는 정점 Z값으로만 조절한다.
    /// </summary>
    void SetInternalOriginSharedPosition(
        double eastWestMeters,
        double northSouthMeters,
        double trueNorthToProjectNorthClockwiseDegrees);
}
