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
    }
}