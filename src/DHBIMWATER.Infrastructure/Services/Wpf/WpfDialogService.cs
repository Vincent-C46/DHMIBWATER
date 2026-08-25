using DHBIMWATER.Application.Interfaces;
using System.Windows;

namespace DHBIMWATER.Infrastructure.Services.Wpf;

/// <summary>전용 WPF UI 스레드에서도 안전하게 사용할 수 있는 정식 다이얼로그 서비스.</summary>
public sealed class WpfDialogService : IDialogService
{
    public void Info(string title, string message) =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public void Warn(string title, string message) =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);

    public bool Confirm(string title, string message) =>
        MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
}
