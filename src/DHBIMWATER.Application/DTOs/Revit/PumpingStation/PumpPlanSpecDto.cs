using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.DTOs.Revit.PumpingStation
{
    public record PumpPlanSpecDto
    (
        double B2,
        double B8,
        string OpeningType,
        double B5,
        double B9,
        double L5,
        double B10
    );
}
