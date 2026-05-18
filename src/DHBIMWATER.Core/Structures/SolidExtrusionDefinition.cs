using DHBIMWATER.Core.Geometry;

namespace DHBIMWATER.Core.Structures
{
    public record SolidExtrusionDefinition
    {
        public IReadOnlyList<Point3D> Profile { get; set; }
        public Vector3D Normal { get; set; }
        public double Distance { get; set; }

        public string ElementCode { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public string Part { get; set; } = string.Empty;
    }
}
