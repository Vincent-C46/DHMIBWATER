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
        /// <summary>프로젝트에 로드된 Adaptive Component 패밀리의 타입명 목록. "패밀리명 : 타입명" 형식으로 반환한다. 직관(2점)·곱관(5점) 목록이 공용한다.</summary>
        IEnumerable<string> GetAdaptiveComponentTypeNames();
        /// <summary>familyTypeName("패밀리명 : 타입명") 인스턴스에 값을 쓸 수 있는 파라미터명 목록. 직경·관종 매핑 후보로 쓴다.</summary>
        IEnumerable<string> GetAdaptiveInstanceParameterNames(string familyTypeName);
        /// <summary>familyTypeName("패밀리명 : 타입명")이 가진 Adaptive Point 개수. 확인할 수 없으면 -1.</summary>
        int GetAdaptiveBendPointCount(string familyTypeName);
        IEnumerable<string> GetPipingSystemTypeNames();
        IEnumerable<string> GetPipeTypeNames();
        IEnumerable<string> GetLevelNames();
        /// <summary>프로젝트에 로드된 배관부속(Pipe Accessory) 패밀리의 타입명 목록. "패밀리명 : 타입명" 형식으로 반환한다.</summary>
        IEnumerable<string> GetPipeAccessoryTypeNames();
        /// <summary>배관부속 familyTypeName("패밀리명 : 타입명") 인스턴스에 값을 쓸 수 있는 파라미터명 목록.
        /// PipeLayout의 단관 길이 파라미터 후보로 쓴다.</summary>
        IEnumerable<string> GetPipeAccessoryInstanceParameterNames(string familyTypeName);
        /// <summary>프로젝트에 로드된 일반모델(Generic Model) 패밀리의 타입명 목록. "패밀리명 : 타입명" 형식으로 반환한다.</summary>
        IEnumerable<string> GetGenericModelTypeNames();
        IEnumerable<string> GetFoundationTypeNames();
        /// <summary>선택된 빔 유형에 배치 가능한 인스턴스 파라미터명 목록. 직경·관종 필드를 패밀리 파라미터에 매핑할 때 후보로 쓴다.</summary>
        IEnumerable<string> GetBeamInstanceParameterNames(string beamTypeName);
    }
}
