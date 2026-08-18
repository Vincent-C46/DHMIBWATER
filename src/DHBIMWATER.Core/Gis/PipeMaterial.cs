namespace DHBIMWATER.Core.Gis;

/// <summary>
/// 관종(재질). <see cref="PipeKindCatalog"/>의 "상수 1종관" 등은 관종이 아니라
/// 덕타일주철관의 두께 <b>등급</b>이므로, 그보다 한 단계 위의 축으로 이것을 둔다.
/// </summary>
/// <remarks>
/// 실제 규격표와 판정 규칙이 있는 관종은 현재 <see cref="DuctileIron"/> 뿐이다.
/// 나머지 값은 확장 지점을 코드에 명시하기 위한 자리로, 규격 자료와 배치 로직이
/// 확보되기 전까지 UI 선택지로 노출하지 않는다(<see cref="PipeKindCatalog.AllFor"/> 참조).
/// </remarks>
public enum PipeMaterial
{
    /// <summary>덕타일주철관. 조인트 허용굴곡 + 표준 곡관으로 판정한다.</summary>
    DuctileIron,

    /// <summary>강관. 제작곡관 기준이라 조인트 허용굴곡 개념을 그대로 쓰지 않는다.</summary>
    Steel,

    /// <summary>PE관. 관 자체가 휘므로 허용굴곡각이 아니라 최소곡률반경으로 판정한다.</summary>
    Pe,

    /// <summary>PVC관. 소켓·접착 접합 기준의 이형관 규격을 따른다.</summary>
    Pvc
}
