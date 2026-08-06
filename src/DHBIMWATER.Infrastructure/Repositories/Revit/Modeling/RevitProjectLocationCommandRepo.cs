using System.Linq;
using Autodesk.Revit.DB;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Infrastructure.Converters;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Modeling;

internal sealed class RevitProjectLocationCommandRepo : IProjectLocationCommandRepo
{
    private readonly Func<Document?> _doc;

    public RevitProjectLocationCommandRepo(Func<Document?> doc) => _doc = doc;

    public void SetInternalOriginSharedPosition(
        double eastWestMeters,
        double northSouthMeters,
        double trueNorthToProjectNorthClockwiseDegrees)
    {
        var doc = _doc() ?? throw new InvalidOperationException("활성 Revit 문서를 찾을 수 없습니다.");

        var basePoint = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_ProjectBasePoint)
            .WhereElementIsNotElementType()
            .FirstOrDefault();
        // PBP는 내부 원점과 같은 위치라는 보장이 없다. 지오메트리는 PBP 기준으로 배치하므로
        // 내부 원점의 공유좌표에서 PBP의 내부좌표 오프셋을 빼야 PBP 공유좌표가 기준점과 일치한다.
        var offset = (basePoint as BasePoint)?.Position ?? XYZ.Zero;

        // Revit API 양의 각도는 반시계방향이다. UI 입력값은 진북→도북 시계방향이므로 부호를 반전한다.
        // TODO: 각도가 0이 아니면 offset도 같은 각도로 회전시켜야 한다(현재 호출부는 항상 0을 넘긴다).
        var angle = -RevitUnitConverter.DegToRad(trueNorthToProjectNorthClockwiseDegrees);
        var position = doc.Application.Create.NewProjectPosition(
            RevitUnitConverter.MToFt(eastWestMeters) - offset.X,
            RevitUnitConverter.MToFt(northSouthMeters) - offset.Y,
            -offset.Z,
            angle);

        doc.ActiveProjectLocation.SetProjectPosition(XYZ.Zero, position);

        // 프로젝트 기준점(Project Base Point)의 표고는 항상 0으로 고정한다(높이는 정점 Z값으로만 조절).
        // 위 계산으로 이미 0이며, 여기서는 확정용 안전장치다.
        basePoint?.get_Parameter(BuiltInParameter.BASEPOINT_ELEVATION_PARAM)?.Set(0.0);
    }
}
