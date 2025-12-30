namespace DHBIMWATER.Core.Reservoirs
{
    // Core 계층
    public class Reservoir
    {
        // 버그 🐞
        public string Name { get; }
        public double CapacityM3 { get; }        // 총 용량 (m3)
        public double MinWaterLevel { get; }      // EL.m
        public double MaxWaterLevel { get; }      // EL.m

        public Reservoir(
            string name,
            double capacityM3,
            double minWaterLevel,
            double maxWaterLevel)
        {
            Name = name;
            CapacityM3 = capacityM3;
            MinWaterLevel = minWaterLevel;
            MaxWaterLevel = maxWaterLevel;
        }

        /// <summary>
        /// 현재 수위가 배수지 운영 범위 안에 있는지 판단
        /// </summary>
        public bool IsWaterLevelInRange(double currentWaterLevel)
        {
            return currentWaterLevel >= MinWaterLevel
                && currentWaterLevel <= MaxWaterLevel;
        }
    }
}
