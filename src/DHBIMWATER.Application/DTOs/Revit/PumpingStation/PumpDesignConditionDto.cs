using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.DTOs.Revit.PumpingStation
{
    public record PumpDesignConditionDto
    (
        string SelectedPumpingStationType,
        string SelectedEntranceType,
        double D,
        double HD,
        double H2,
        int N,
        double LWL,
        double HWL
    );
}