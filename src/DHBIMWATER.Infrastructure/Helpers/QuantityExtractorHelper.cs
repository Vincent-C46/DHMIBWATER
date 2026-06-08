using DHBIMWATER.Core.Quantity;

namespace DHBIMWATER.Infrastructure.Helpers
{
    internal static class QuantityExtractorHelper
    {
        public static Dictionary<FaceType, List<(FaceType FaceType, long NeighborId, double Area)>> GroupDeductions(
            IReadOnlyList<(FaceType FaceType, long NeighborId, double Area)> contactAreas)
            => contactAreas
                .GroupBy(d => d.FaceType)
                .ToDictionary(g => g.Key, g => g.ToList());

        public static double GetNetArea(
            IReadOnlyDictionary<FaceType, double> faceDict,
            IReadOnlyDictionary<FaceType, List<(FaceType FaceType, long NeighborId, double Area)>> deductions,
            FaceType faceType)
        {
            var gross = faceDict.GetValueOrDefault(faceType, 0);
            var deductTotal = deductions.TryGetValue(faceType, out var list)
                                        ? list.Sum(d => d.Area)
                                        : 0;
            return Math.Max(0, gross - deductTotal);
        }

        public static string GetDeductionFormula(
            IReadOnlyDictionary<FaceType, double> faceDict,
            Dictionary<FaceType, List<(FaceType FaceType, long NeighborId, double Area)>> deductions,
            FaceType faceType)
        {
            var gross = faceDict.GetValueOrDefault(faceType, 0);

            if (!deductions.TryGetValue(faceType, out var deducts) || gross == 0)
                return $"{gross:F3}";

            var deductParts = deducts
               .Select(d => $"{d.Area:F3} (Id_{d.NeighborId})");

            return $"{gross:F3}(A) - " + string.Join(" - ", deductParts);
        }
    }
}
