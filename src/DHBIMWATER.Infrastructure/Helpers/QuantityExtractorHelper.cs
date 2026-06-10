using DHBIMWATER.Core.Quantity;

namespace DHBIMWATER.Infrastructure.Helpers
{
    internal static class QuantityExtractorHelper
    {
        public static IReadOnlyDictionary<FaceType, List<FaceDeduction>> GroupDeductions(IReadOnlyList<FaceDeduction> contactAreas)
            => contactAreas
                .GroupBy(d => d.TargetFaceType)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(d => d.NeighborId)
                           .Select(ng => new FaceDeduction(g.Key, ng.Key, ng.Sum(d => d.Area)))
                           .ToList());

        public static double GetNetArea(
            IReadOnlyDictionary<FaceType, double> faceDict,
            IReadOnlyDictionary<FaceType, List<FaceDeduction>> deductions,
            FaceType faceType)
        {
            var gross = faceDict.GetValueOrDefault(faceType, 0);
            var deductTotal = deductions.TryGetValue(faceType, out var list)
                                        ? list.Sum(d => d.Area)
                                        : 0;
            return Math.Max(0, gross - deductTotal);
        }

        public static string GetDeductionRawFormula(
            IReadOnlyDictionary<FaceType, double> faceDict,
            IReadOnlyDictionary<FaceType, List<FaceDeduction>> deductions,
            FaceType faceType)
        {
            var gross = faceDict.GetValueOrDefault(faceType, 0);

            if (!deductions.TryGetValue(faceType, out var deducts) || gross == 0)
                return "A";

            var deductParts = deducts.Select((_, i) => $"A{i + 1}");
            return "A - " + string.Join(" - ", deductParts);
        }

        public static string GetDeductionRenderedFormula(
            IReadOnlyDictionary<FaceType, double> faceDict,
            IReadOnlyDictionary<FaceType, List<FaceDeduction>> deductions,
            FaceType faceType)
        {
            var gross = faceDict.GetValueOrDefault(faceType, 0);

            if (!deductions.TryGetValue(faceType, out var deducts) || gross == 0)
                return $"{gross:F3}(A)";

            //var deductParts = deducts.Select((d, i) => $"{d.Area:F3}(A{i + 1}: {d.NeighborId})");
            var deductParts = deducts.Select((d, i) => $"{d.Area:F3}(A{i + 1})");
            return $"{gross:F3}(A) - " + string.Join(" - ", deductParts);
        }
    }
}
