using DHBIMWATER.Application.Interfaces;
using System.Windows;

namespace DHBIMWATER.Infrastructure.Services.Mock
{
    /// <summary>
    /// Mock 다이얼로그 서비스 (Sandbox/테스트 환경용)
    /// Revit API 대신 WPF MessageBox 사용
    /// </summary>
    public class MockDialogService : IDialogService
    {
        /// <summary>
        /// 정보 메시지 표시 (MessageBox)
        /// </summary>
        public void Info(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 경고 메시지 표시 (MessageBox)
        /// </summary>
        public void Warn(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>
        /// 확인 다이얼로그 표시 (MessageBox - Yes/No)
        /// </summary>
        public bool Confirm(string title, string message)
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }
    }
}
