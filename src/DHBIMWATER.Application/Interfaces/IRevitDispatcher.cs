namespace DHBIMWATER.Application.Interfaces;

/// <summary>Revit API 접근을 Revit 메인 스레드에서 동기 실행한다.</summary>
public interface IRevitDispatcher
{
    void Run(Action action);
    T Run<T>(Func<T> func);
}
