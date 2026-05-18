using DHBIMWATER.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Core.Structures
{
    public class SectionViewDefinition
    {
        public required string Name { get; init; } = string.Empty;

        public Vector3D Origin { get; init; } = new Vector3D(0,0,0);
        public Vector3D BasisX { get; init; }
        public Vector3D BasisY { get; init; }
        public Vector3D BasisZ { get; init; }

        public Point3D Min { get; init; }
        public Point3D Max { get; init; }

        public bool Flip { get; init; } = false;    
    }
}