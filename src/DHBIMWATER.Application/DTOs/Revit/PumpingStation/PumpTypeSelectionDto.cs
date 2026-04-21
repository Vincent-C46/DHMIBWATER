using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.DTOs.Revit.PumpingStation
{
    public record PumpTypeSelectionDto
    (
        double T1,
        double T2,
        double T3,
        double T4,
        double T5,
        double Gb1,
        double Gh1
    );
}