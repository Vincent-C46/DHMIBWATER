using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.Interfaces
{
    public interface IElementTypeQueryRepo
    {
        IEnumerable<string> GetSlabTypeNames();
        IEnumerable<string> GetWallTypeNames();
        IEnumerable<string> GetColumnTypeNames();
        IEnumerable<string> GetBeamTypeNames();
        IEnumerable<string> GetFoundationTypeNames();
    }
}
