using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.Interfaces
{
    public interface ILevelCommandRepo
    {
        long CreateLevel(string levelName, double elevation);
        long UpdateLevel(string levelName, double elevation);
        void CreatePlan(long levelId);
        // 레벨(데이텀)의 3D 범위를 모델에 맞게 최대화 (우클릭 "3D 범위 최대화"와 동일)
        void Maximize3dExtents(long levelId);
    }
}
