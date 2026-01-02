using DHBIMWATER.Application.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.DTOs.Revit.Reservoir
{
    public class CreateReservoirWallDto
    {
        public Point3DDto StartPt { get; set; }
        public Point3DDto EndPt { get; set; }
        public double Length { get; set; }
    }
}
