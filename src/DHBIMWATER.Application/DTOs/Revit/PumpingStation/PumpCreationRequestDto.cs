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
        PumpProfileSpecDto ProfileSpecDto,
        // HasCheckValve 여부에 따라 선택된 밸브받침 제원 (WithCheckValve / WithoutCheckValve 중 하나)
        PumpValveDimensionDto ValveBase,
        PumpMaterialSpecDto Materials
        //PumpTypeSelectionDto TypeSelectionDto
    );
}
