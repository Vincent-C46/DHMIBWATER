using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.DTOs.Revit.PumpingStation
{
    public record PumpDesignConditionDto
    (
        string PumpingStationType,
        string EntranceType,
        double D,
        double HD,
        int N,
        double LWL,
        double HWL
    );
}