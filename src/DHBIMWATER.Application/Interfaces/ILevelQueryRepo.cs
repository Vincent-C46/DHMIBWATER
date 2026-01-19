using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.Interfaces
{
    public interface ILevelQueryRepo
    {
        IEnumerable<string> GetExistingLevelNames();
        IEnumerable<string> GetExistingPlanNames();
    }
}
