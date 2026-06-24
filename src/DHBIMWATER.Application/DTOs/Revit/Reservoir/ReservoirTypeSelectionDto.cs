using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.DTOs.Revit.Reservoir
{
    /// <summary>
    /// 유형 선택 대신 두께를 직접 입력받아 Infrastructure에서 유형을 자동 생성
    /// 단위: mm
    /// </summary>
    public record ReservoirTypeThicknessDto
    (
        // 슬래브 두께
        double StuThk,   // 수조부 상부슬래브
        double StbThk,   // 수조부 하부슬래브(기초)
        double SvuThk,   // 밸브실 상부슬래브
        double SvmThk,   // 밸브실 중간슬래브
        double SvbThk,   // 밸브실 하부슬래브(기초)

        // 벽체 두께
        double WteThk,   // 수조부 외벽
        double WtiThk,   // 수조부 내벽
        double WhThk,    // 호퍼 벽체
        double WveThk,   // 밸브실 외벽
        double WviThk,   // 밸브실 내벽
        double LcThk,    // 버림콘크리트 두께

        // 기둥 단면 (mm)
        double Cw,       // 기둥 폭
        double Cd,       // 기둥 깊이

        // 보 단면 (mm)
        double Gw,       // 보 폭
        double Gh        // 보 높이
    );
}
