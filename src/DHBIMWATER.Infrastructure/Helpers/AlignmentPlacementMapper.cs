using Autodesk.Revit.DB;
using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Gis;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Helpers;

/// <summary>
/// 선형 정점(원본 좌표계, m)을 Revit 내부좌표(ft)로 변환한다. 선형 배치 리포지토리 3종이 공유한다.
/// 배치 규칙:
///  - XY: 기준점이 프로젝트 기준점(PBP) 위치에 정확히 놓이도록 PBP 기준 상대 배치한다.
///  - Z : 기준 표고를 빼지 않고 정점 Z(ZDatum 보정 후)를 PBP 기준 상대 높이로 그대로 사용한다.
///        PBP 표고는 항상 0이므로 "모델 높이 = 정점 Z값"이 성립한다.
/// </summary>
public static class AlignmentPlacementMapper
{
    /// <summary>PBP의 내부좌표(ft). PBP를 찾지 못하면 내부 원점으로 간주한다.</summary>
    public static XYZ GetProjectBasePoint(Document doc)
    {
        var basePoint = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_ProjectBasePoint)
            .WhereElementIsNotElementType()
            .FirstOrDefault() as BasePoint;
        return basePoint?.Position ?? XYZ.Zero;
    }

    /// <param name="basePoint">GetProjectBasePoint 결과. 정점마다 재조회하지 않도록 호출부에서 1회 조회해 넘긴다.</param>
    public static XYZ ToXyz(Point3D point, double diameterMm, double referenceX, double referenceY, ZDatum zDatum, XYZ basePoint)
    {
        var z = zDatum switch
        {
            ZDatum.Invert => point.Z + diameterMm / 2000.0,
            ZDatum.Crown => point.Z - diameterMm / 2000.0,
            _ => point.Z
        };
        return new XYZ(
            basePoint.X + UC.MToFt(point.X - referenceX),
            basePoint.Y + UC.MToFt(point.Y - referenceY),
            basePoint.Z + UC.MToFt(z));
    }
}
