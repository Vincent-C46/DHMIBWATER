using DHBIMWATER.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Core.Structures
{
    public record SlabDefinition
    {
        public IReadOnlyList<Point2D> Points {get; set;}
        public double Thickness { get; set; }
        public double ElevationZ { get; set; }

        public double Area => PolygonCalculator.ComputeArea(Points);
        public double Perimeter => PolygonCalculator.ComputePerimeter(Points);
        public double Volume => Area * Thickness;
    }
}
