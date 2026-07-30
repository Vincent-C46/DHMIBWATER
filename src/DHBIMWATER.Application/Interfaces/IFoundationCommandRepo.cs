using DHBIMWATER.Core.Structures;

namespace DHBIMWATER.Application.Interfaces;

/// <summary>현재 문서에 로드된 독립기초 유형을 복제하고 새 인스턴스를 배치한다.</summary>
public interface IFoundationCommandRepo
{
    int CreateFoundationFromFirstInstance(FoundationDefinition definition);
}
