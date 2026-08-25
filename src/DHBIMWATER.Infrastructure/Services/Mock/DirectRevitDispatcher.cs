using DHBIMWATER.Application.Interfaces;

namespace DHBIMWATER.Infrastructure.Services.Mock;

/// <summary>Revit 스레드 제약이 없는 테스트·샌드박스용 즉시 실행 구현.</summary>
public sealed class DirectRevitDispatcher : IRevitDispatcher
{
    public void Run(Action action) => action();
    public T Run<T>(Func<T> func) => func();
}
