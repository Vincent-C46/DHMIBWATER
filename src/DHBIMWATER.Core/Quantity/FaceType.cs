using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Core.Quantity
{
    public enum FaceType
    {
        None,
        Top,
        Bottom,
        Side,
        Left,   // 진행방향의 오른쪽 (벽, 보 전용)
        Right,
        End,    // 진행방향의 양단부 (벽 마구리면, 캔틸레버 보)
    }
}