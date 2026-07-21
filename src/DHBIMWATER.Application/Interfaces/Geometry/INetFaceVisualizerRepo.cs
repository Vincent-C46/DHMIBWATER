namespace DHBIMWATER.Application.Interfaces.Geometry
{
    /// <summary>선택 요소의 공제 후 순수 노출면을 Revit에 시각화한다.</summary>
    public interface INetFaceVisualizerRepo
    {
        int Visualize(IReadOnlyList<long> elementIds);
    }
}
