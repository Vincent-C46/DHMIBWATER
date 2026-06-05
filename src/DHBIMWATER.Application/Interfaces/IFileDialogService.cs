using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.Interfaces
{
    public interface IFileDialogService
    {
        string? OpenFile(string title, string filter);
        string? SaveFile(string title, string filter, string defaultFileName = "");
    }
}