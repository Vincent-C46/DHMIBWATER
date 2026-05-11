using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.DTOs.Revit.Reservoir
{
    public record ReservoirSelectedTypeIdDto
     (
        string TankUpperSlabTypeId,
        string TankFndSlabTypeId,
        string TankOuterWallTypeId,
        string TankInnerWallTypeId,
        string TankColumnTypeId,
        string TankBeamTypeId,
        string ValveUpperSlabTypeId,
        string ValveMidSlabTypeId,
        string ValveFndSlabTypeId,
        string ValveOuterWallTypeId,
        string SubFndSlabTypeId
    );
}
