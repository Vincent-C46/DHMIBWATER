namespace DHBIMWATER.Core.Gis;

/// <summary>원본 Z를 관 중심선 Z로 환산하는 오프셋(mm)을 계산한다.</summary>
public static class PipeElevationDatum
{
    /// <param name="spec">직관 제원. null이면 DN을 외경으로 간주하는 폴백이다.</param>
    /// <returns>원본 Z에 더할 값(mm).</returns>
    public static double OffsetMm(ZDatum datum, StraightPipeSpec? spec, double nominalDiameterMm)
    {
        if (datum == ZDatum.Centerline) return 0d;

        var od = spec?.OuterDiameterMm > 0 ? spec.OuterDiameterMm : nominalDiameterMm;
        var outerRadius = od / 2d;
        var innerRadius = spec is not null ? (od - 2d * spec.ThicknessMm) / 2d : outerRadius;
        return datum switch
        {
            ZDatum.Invert => innerRadius,
            ZDatum.Crown => -innerRadius,
            ZDatum.OutsideBottom => outerRadius,
            ZDatum.OutsideTop => -outerRadius,
            _ => 0d
        };
    }

    /// <summary>제원 없이 보정하면 높이가 부정확한 경우.</summary>
    public static bool NeedsSpec(ZDatum datum) => datum != ZDatum.Centerline;
}
