using DHBIMWATER.Core.Geometry;
using DHBIMWATER.UI.Base;

namespace DHBIMWATER.UI.ViewModels.Modeling;

/// <summary>
/// 선형(레코드) 1건의 진행 방향 지정 행. <see cref="IsReversed"/>가 참이면 배치 시 정점 순서를 뒤집어
/// 시작점과 끝점을 교환한다. 좌표값 자체는 바꾸지 않는다.
/// </summary>
public sealed class AlignmentDirectionRow : ViewModelBase
{
    private bool _isReversed, _isSelected;

    /// <summary>파일 경로 + 시트명. 엑셀은 시트마다 레코드번호가 "1"로 겹치므로 파일 경로만으로는 구분되지 않는다.</summary>
    public required string FileKey { get; init; }
    public required string FileName { get; init; }
    public required string RecordNumber { get; init; }
    /// <summary>원본 파일에 기록된 순서의 첫 정점. 반전 여부와 무관하게 고정이다.</summary>
    public required Point3D SourceStart { get; init; }
    public required Point3D SourceEnd { get; init; }
    public required int VertexCount { get; init; }
    /// <summary>폴리선 전체 길이(m). 반전과 무관하므로 한 번만 계산해 둔다.</summary>
    public required double LengthM { get; init; }

    public bool IsReversed
    {
        get => _isReversed;
        set
        {
            if (!SetProperty(ref _isReversed, value)) return;
            OnPropertyChanged(nameof(StartDisplay)); OnPropertyChanged(nameof(EndDisplay));
            Changed?.Invoke();
        }
    }
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
    public Action? Changed { get; init; }

    /// <summary>체크 즉시 시작·끝 표시가 바뀌어야 사용자가 방향을 확인할 수 있다.</summary>
    public string StartDisplay => Format(IsReversed ? SourceEnd : SourceStart);
    public string EndDisplay => Format(IsReversed ? SourceStart : SourceEnd);
    public string LengthDisplay => $"{LengthM:0.##}";
    public string VertexDisplay => $"{VertexCount:N0}";

    private static string Format(Point3D point) => $"{point.X:0.###}, {point.Y:0.###}, {point.Z:0.###}";
}
