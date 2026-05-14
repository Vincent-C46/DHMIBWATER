using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.Interfaces
{
    public interface ILevelCommandRepo
    {
        int CreateLevel(string levelName, double elevation);
        int UpdateLevel(string levelName, double elevation);
        void CreatePlan(int levelId);
    }
}
