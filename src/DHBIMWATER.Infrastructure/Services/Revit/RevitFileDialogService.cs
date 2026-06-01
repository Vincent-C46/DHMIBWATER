using DHBIMWATER.Application.Interfaces;
using Microsoft.Win32;

namespace DHBIMWATER.Infrastructure.Services.Revit
{
    public class RevitFileDialogService : IFileDialogService
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
            var dialog = new SaveFileDialog
            {
                Title = title,
                Filter = filter,
                FileName = defaultFileName
            };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }
    }
}