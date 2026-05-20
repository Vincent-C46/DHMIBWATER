namespace DHBIMWATER.Core.Structures

{   /// <param name="UnitWeightKgPerM">단위중량 (kg/m)</param>
    /// <param name="NominalDiameterMm">공칭지름 (mm)</param>
    /// <param name="NominalAreaMm2">공칭단면적 (mm²)</param>
    public record RebarSpec(double UnitWeightKgPerM, double NominalDiameterMm, double NominalAreaMm2);

    public static class RebarDatabase
    {
        // KS D 3504 : 철근콘크리트용 봉강
        public static class KSD3504
        {
            public static readonly IReadOnlyDictionary<string, RebarSpec> All =
                new Dictionary<string, RebarSpec>
                {
                    ["D6"] = new(0.249, 6.35, 31.67),
                    ["D10"] = new(0.56, 9.53, 71.33),
                    ["D13"] = new(0.995, 12.70, 126.70),
                    ["D16"] = new(1.560, 15.90, 198.60),
                    ["D19"] = new(2.250, 19.10, 286.50),
                    ["D22"] = new(3.040, 22.20, 387.10),
                    ["D25"] = new(3.980, 25.40, 506.70),
                    ["D29"] = new(5.040, 28.60, 642.40),
                    ["D32"] = new(6.230, 31.80, 794.20),
                    ["D35"] = new(7.510, 34.9, 956.60),
                    ["D38"] = new(8.950, 38.1, 1140.0),
                    ["D41"] = new(10.500, 41.3, 1340.00),
                    ["D51"] = new(15.900, 50.80, 2027.0),
                };

            public static RebarSpec Get(string key) => All[key];
        }

        // ASTM A615 : Standard Specification for Deformed and Plain Carbon-Steel Bars
        public static class ASTMA615
        {
            public static readonly IReadOnlyDictionary<string, RebarSpec> All =
                new Dictionary<string, RebarSpec>
                {
                    ["#3"]  = new( 0.560,  9.5,   71),
                    ["#4"]  = new( 0.994, 12.7,  129),
                    ["#5"]  = new( 1.552, 15.9,  199),
                    ["#6"]  = new( 2.235, 19.1,  284),
                    ["#7"]  = new( 3.042, 22.2,  387),
                    ["#8"]  = new( 3.973, 25.4,  510),
                    ["#9"]  = new( 5.060, 28.7,  645),
                    ["#10"] = new( 6.404, 32.3,  819),
                    ["#11"] = new( 7.907, 35.8, 1006),
                    ["#14"] = new( 11.38, 43.0, 1452),
                    ["#18"] = new( 20.24, 57.3, 2581),
                };

            public static RebarSpec Get(string key) => All[key];
        }

        // BS 4449 : Steel for the reinforcement of concrete
        public static class BS4449
        {
            public static readonly IReadOnlyDictionary<string, RebarSpec> All =
                new Dictionary<string, RebarSpec>
                {
                    ["P5"]  = new( 0.154,  5.0,   19.60),
                    ["P6"]  = new( 0.222,  6.0,   28.27),
                    ["P7"]  = new( 0.302,  7.0,   38.50),
                    ["P8"]  = new( 0.395,  8.0,   50.27),
                    ["P9"]  = new( 0.499,  9.0,   63.60),
                    ["P10"] = new( 0.617, 10.0,   78.54),
                    ["P11"] = new( 0.746, 11.0,   95.00),
                    ["P12"] = new( 0.888, 12.0,  113.10),
                    ["P13"] = new( 1.043, 13.0,  132.70),
                    ["P16"] = new( 1.578, 16.0,  201.06),
                    ["P20"] = new( 2.466, 20.0,  314.16),
                    ["P25"] = new( 3.853, 25.0,  490.87),
                    ["P32"] = new( 6.313, 32.0,  804.25),
                    ["P40"] = new( 9.865, 40.0, 1256.64),
                };

            public static RebarSpec Get(string key) => All[key];
        }
    }
}
