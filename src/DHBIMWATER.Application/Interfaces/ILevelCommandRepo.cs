using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.Interfaces
{
    public interface ILevelCommandRepo
    {
        void CreateLevel(string levelName, double elevation);
        void UpdateLevel(string levelName, double elevation);
    }
}
