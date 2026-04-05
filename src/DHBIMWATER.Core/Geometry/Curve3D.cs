using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Core.Geometry
{
    public abstract class Curve3D
    {
        public abstract Point3D StartPoint { get; }
        public abstract Point3D EndPoint { get; }
        public abstract double Length { get; }
    }
}