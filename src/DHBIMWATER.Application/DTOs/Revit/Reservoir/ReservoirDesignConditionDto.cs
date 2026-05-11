using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.DTOs.Revit.Reservoir
{
    public record ReservoirDesignConditionDto
    (
        double Q,
        double RT,
        int N,
        double LWL
    );
}
