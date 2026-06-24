using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.DTOs.Revit.Reservoir
{
    /// <summary>
    /// 밸브실(배관실) 설계 조건
    /// 단위: 미터(m)
    /// </summary>
    public record ReservoirValveDto
    (
        double H1F,    // 밸브실 1층 바닥~중간슬래브 순높이 (m)
        double Lv,     // 밸브실 길이 (m)
        double Wv,     // 밸브실 폭 (m)
        double Lvt,    // 밸브실 toe 길이 (m)
        double We,     // 전기실 폭 (m)
        double TrOff,  // 트렌치 옵셋 (m)
        double Wp,     // 파이프 공간 폭 (m)
        double Hp,     // 파이프 공간 높이 (m)
        double WpThk,  // 파이프 슬래브 두께 (m)
        double SpThk,  // 밸브실 바닥 스트럿 두께 (m)
        double SLp     // 밸브실 배관레벨 (m)
    );
}
