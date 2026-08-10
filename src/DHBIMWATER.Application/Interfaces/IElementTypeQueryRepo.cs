using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.Interfaces
{
    public interface IElementTypeQueryRepo
    {
        IEnumerable<string> GetSlabTypeNames();
        IEnumerable<string> GetWallTypeNames();
        IEnumerable<string> GetColumnTypeNames();
        IEnumerable<string> GetBeamTypeNames();
        IEnumerable<string> GetPipingSystemTypeNames();
        IEnumerable<string> GetPipeTypeNames();
        IEnumerable<string> GetFoundationTypeNames();
        /// <summary>선택된 빔 유형에 배치 가능한 인스턴스 파라미터명 목록. 직경·관종 필드를 패밀리 파라미터에 매핑할 때 후보로 쓴다.</summary>
        IEnumerable<string> GetBeamInstanceParameterNames(string beamTypeName);
    }
}
