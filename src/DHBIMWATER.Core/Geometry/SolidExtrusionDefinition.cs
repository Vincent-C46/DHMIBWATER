namespace DHBIMWATER.Core.Geometry
{
    public record SolidExtrusionDefinition
    {
        public IReadOnlyList<Point3D> Profile { get; set; }
        public IReadOnlyList<VoidExtrusionDefinition> Voids { get; set; } = Array.Empty<VoidExtrusionDefinition>();
        public Vector3D Normal { get; set; }
        public double Distance { get; set; }
        
        public string Category { get; set; } = "DirectShape";
        public string ElementCode { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public string Part { get; set; } = string.Empty;
    }

    public record VoidExtrusionDefinition
    {
        public IReadOnlyList<Point3D> Profile { get; set; }
        public Vector3D Normal { get; set; }
        public double Distance { get; set; }
    }
}
