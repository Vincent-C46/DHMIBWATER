using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.DTOs.Revit.Reservoir
{
    public record ReservoirValveDto
     (
        double H1,
        double H2,
        double Lv,
        double Wv
    );
}
