using DHBIMWATER.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Core.Structures
{
    public record ProfileWallDefinition
    {
        public IReadOnlyList<Point3D> Points { get; set; }

    }
}