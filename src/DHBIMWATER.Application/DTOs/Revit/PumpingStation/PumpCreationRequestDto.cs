using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.DTOs.Revit.PumpingStation
{
    public record PumpCreationRequestDto
    (
        PumpDesignConditionDto DesignConditionDto,
        PumpPlanSpecDto PlanSpecDto,
        PumpProfileSpecDto ProfileSpecDto
        //PumpTypeSelectionDto TypeSelectionDto
    );
}
