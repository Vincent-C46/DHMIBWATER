using DHBIMWATER.Core.Piping;

namespace DHBIMWATER.Application.Interfaces
{
    /// <summary>
    /// Revit 뷰에서 밸브실 외곽 벽체를 사용자에게 직접 피킹시켜 레이아웃 외곽을 만든다.
    /// 반환 좌표는 <b>Revit 프로젝트 좌표 mm</b>이며, 캔버스 좌표 변환은 호출측(ViewModel)이 담당한다.
    /// </summary>
    public interface IValveRoomOutlinePickRepo
    {
        /// <summary>사용자가 벽을 고르지 않고 ESC로 취소하면 null을 반환한다.</summary>
        ValveRoomOutline? PickExteriorWalls();
    }
}
