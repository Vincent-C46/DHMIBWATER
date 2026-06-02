using DHBIMWATER.Application.Interfaces;
using Microsoft.Win32;

namespace DHBIMWATER.Infrastructure.Services.Mock
{
    public class MockFileDialogService : IFileDialogService
    {
        public string? OpenFile(string title, string filter)
        {
            var dialog = new OpenFileDialog
            {
                Title = title,
                Filter = filter
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public string? SaveFile(string title, string filter, string defaultFileName = "")
        {
            return string.Empty;
        }
    }
}
