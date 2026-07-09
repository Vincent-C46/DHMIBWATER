namespace DHBIMWATER.Application.Interfaces.Quantity
{
    /// <summary>측정 종류 - 길이(폴리라인) 또는 면적(면 선택)</summary>
    public enum MeasureKind
    {
        Length,
        Area
    }

    /// <summary>측정 결과. Value는 프로젝트 표시값, Unit은 "m"/"m²".</summary>
    public record MeasureResult(double Value, string Unit);

    /// <summary>
    /// Revit 뷰에서 길이/면적을 측정해 값을 돌려주는 서비스.
    /// Revit 환경에서는 ExternalEvent 기반으로 구현되며, 취소(ESC) 시 null을 반환한다.
    /// </summary>
    public interface IMeasurePickService
    {
        /// <summary>측정 요청. 취소되면 null.</summary>
        Task<MeasureResult?> PickAsync(MeasureKind kind);
    }
}
