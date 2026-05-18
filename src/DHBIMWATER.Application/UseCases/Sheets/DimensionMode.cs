using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.UseCases.Sheets
{
    public enum DimensionMode
    {
        AllObjects,
        SelectedObjects
    }

    [System.Flags]
    public enum DimensionSide
    {
        None   = 0,
        Top    = 1,
        Bottom = 2,
        Left   = 4,
        Right  = 8,
        All    = Top | Bottom | Left | Right
    }
}
