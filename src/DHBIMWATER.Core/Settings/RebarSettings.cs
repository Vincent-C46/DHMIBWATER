namespace DHBIMWATER.Core.Settings
{
    public class RebarSettings
    {
        // 겹이음 길이 (mm) — key: 철근 지름 (D10, D13, ...)
        public Dictionary<string, int> LapSpliceMm { get; set; } = new()
        {
            ["D10"] = 300, ["D13"] = 400, ["D16"] = 500,
            ["D19"] = 600, ["D22"] = 700, ["D25"] = 800
        };

        // 정착 길이 (mm)
        public Dictionary<string, int> DevelopmentLengthMm { get; set; } = new()
        {
            ["D10"] = 250, ["D13"] = 320, ["D16"] = 400,
            ["D19"] = 480, ["D22"] = 560, ["D25"] = 640
        };
    }
}
