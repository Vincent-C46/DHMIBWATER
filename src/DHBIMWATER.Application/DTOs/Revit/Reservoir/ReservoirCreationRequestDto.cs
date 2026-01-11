using DHBIMWATER.Application.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.DTOs.Revit.Reservoir
{
    public record ReservoirCreationRequestDto
    (
        ReservoirDesignConditionDto DesignConditionDto,
        ReservoirTankDto TankDto,
        ReservoirValveDto ValveDto,
        ReservoirSelectedTypeIdDto TypeIdDto
    );
}
