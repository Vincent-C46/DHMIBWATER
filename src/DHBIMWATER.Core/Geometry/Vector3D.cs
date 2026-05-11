namespace DHBIMWATER.Core.Geometry
{
    public record Vector3D(double X, double Y, double Z)
    {
        public static Vector3D UnitX => new(1, 0, 0);
        public static Vector3D UnitY => new(0, 1, 0);
        public static Vector3D UnitZ => new(0, 0, 1);

        public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);

        public Vector3D Normalize()
        {
            var len = Length;
            return new Vector3D(X / len, Y / len, Z / len);
        }
    }
}
