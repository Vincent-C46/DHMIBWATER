using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.DTOs.Revit.Reservoir
{
   public record ReservoirDesignConditionDto
    {
        public double Q { get; init; }
        public double RT { get; init; }
        public int N { get; init; }
        public double LWL { get; init; }
    }
}
