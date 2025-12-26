namespace DHBIMWATER.Application.Interface
{
    // Revit과 SandBox 환경에서 TaskDialog와 MessageBox를 사용하기 위해 추상화된 인터페이스
    // 해당 인터페이스를 구현시 Revit 환경에서는 TaksDialog를, SandBox 환경에서는 MessageBox를 사용하도록 구현
    public interface IDialogService
    {
        void Info(string title, string message);
        void Warn(string title, string message);
        bool Confirm(string title, string message);
    }
}
