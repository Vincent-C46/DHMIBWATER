using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.DTOs.Revit.Reservoir
{
    public record ReservoirTankDto
     (
        double He,
        double Hf,
        double Hm,
        double W,
        double L,
        double M1,
        double M2,
        double M3,
        double M4,
        double Wh,
        double Lh,
        double Hh,
        double Lt
    );
}
