using System.IO;

namespace DHBIMWATER.Infrastructure.Repositories.Gis;

internal static class PrjFileReader
{
    public static (string? Wkt, string? RecommendedEpsg) Read(string path)
    {
        if (!File.Exists(path)) return (null, null);
        var wkt = File.ReadAllText(path);
        return (wkt, wkt.Contains("Central_Belt_2010", StringComparison.OrdinalIgnoreCase) || wkt.Contains("5186", StringComparison.OrdinalIgnoreCase) ? "EPSG:5186" : null);
    }
}
