namespace DHBIMWATER.Application.Interfaces;

/// <summary>활성 Revit 프로젝트의 공유좌표 상태를 조회한다.</summary>
public interface IProjectLocationQueryRepo
{
    /// <summary>프로젝트 기준점(PBP)의 현재 공유좌표(동서, 남북, m)를 읽는다.</summary>
    (double EastWestMeters, double NorthSouthMeters) GetProjectBasePointSharedPosition();
}
