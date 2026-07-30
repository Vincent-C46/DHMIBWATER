using Autodesk.Revit.DB;
using DHBIMWATER.Application.DTOs.Revit.ValveRoom;
using DHBIMWATER.Application.Interfaces;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Modeling;

/// <summary><c>void_면기반</c>을 공기밸브실 독립기초의 X/Y 측면에 호스팅한다.</summary>
public sealed class RevitAirValveVoidCommandRepo : IAirValveVoidCommandRepo
{
    private const string FamilyName = "void_면기반";
    private readonly Func<Document?> _doc;

    public RevitAirValveVoidCommandRepo(Func<Document?> doc) => _doc = doc;

    public void CreateAirValveFoundationVoid(int foundationElementId, AirValveVoidPlacementDefinition definition)
    {
        var doc = _doc() ?? throw new InvalidOperationException("활성 Revit 문서를 찾을 수 없습니다.");
        var foundation = doc.GetElement(new ElementId(foundationElementId))
            ?? throw new InvalidOperationException("공기밸브실 독립기초를 찾을 수 없습니다.");
        var symbol = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).WhereElementIsElementType().OfType<FamilySymbol>()
            .FirstOrDefault(item => item.FamilyName.Equals(FamilyName, StringComparison.OrdinalIgnoreCase));
        if (symbol == null) throw new InvalidOperationException($"면기반 Void 패밀리 '{FamilyName}'을(를) 프로젝트에 로드해야 합니다.");
        if (!symbol.IsActive) { symbol.Activate(); doc.Regenerate(); }

        var box = foundation.get_BoundingBox(null) ?? throw new InvalidOperationException("독립기초의 형상 정보를 읽을 수 없습니다.");
        var isXAxis = definition.Axis.Equals("X", StringComparison.OrdinalIgnoreCase);
        // void_면기반의 Void는 호스팅한 외곽면에서 기초 내부 쪽으로 돌출한다.
        // 따라서 X/Y 관통 모두 음수 방향 외곽면을 시작면으로 사용한다.
        var targetNormal = isXAxis ? -XYZ.BasisX : -XYZ.BasisY;
        var desiredCenter = CreateDesiredCenter(box, definition.PipeCenter, isXAxis);
        var face = FindPlanarFace(foundation, targetNormal, desiredCenter)
            ?? throw new InvalidOperationException(
                $"독립기초에서 {definition.Axis}축 Void를 위한 시작면(법선 {Format(targetNormal)}, 중심 {Format(desiredCenter)})을 찾을 수 없습니다. " +
                $"검출된 평면 법선: {DescribePlanarNormals(foundation)}");
        var projected = face.Project(desiredCenter)
            ?? throw new InvalidOperationException($"선택한 {definition.Axis}축 Void 시작면에 관통 중심을 투영할 수 없습니다.");
        var position = projected.XYZPoint;
        var instance = doc.Create.NewFamilyInstance(face.Reference, position, XYZ.BasisZ, symbol);
        var radius = instance.LookupParameter("r");
        if (radius == null || radius.IsReadOnly) throw new InvalidOperationException($"면기반 Void 패밀리 '{FamilyName}'에서 읽기 가능한 인스턴스 매개변수 'r'을 찾을 수 없습니다.");
        radius.Set(UC.MmToFt(definition.Radius));
        instance.LookupParameter("DH_Addin")?.Set("DHBIMWATER");
        instance.LookupParameter("DH_Category")?.Set("개구부");
        instance.LookupParameter("DH_Part")?.Set("본관 관통 Void");
    }

    /// <summary>법선 일치 판정 허용오차. 기초 회전(-90°) 시 발생하는 부동소수 오차를 흡수한다.</summary>
    private const double NormalTolerance = 0.999;

    /// <summary>
    /// 관통 중심이 실제 Face 경계 안에 들어가는 시작면만 선택한다.
    /// BoundingBox 좌표는 회전한 로드 패밀리의 실제 면 위치를 대변하지 않으므로 배치점으로 쓰지 않는다.
    /// </summary>
    private static PlanarFace? FindPlanarFace(Element host, XYZ targetNormal, XYZ desiredCenter)
    {
        foreach (var planar in EnumeratePlanarFaces(host))
        {
            if (planar.FaceNormal.Normalize().DotProduct(targetNormal) <= NormalTolerance) continue;
            var projection = planar.Project(desiredCenter);
            if (projection != null && planar.IsInside(projection.UVPoint)) return planar;
        }
        return null;
    }

    /// <summary>관통 중심의 평면 좌표를 만들고, 시작면 법선 좌표는 기초 중심으로 둬 Face 투영으로 확정한다.</summary>
    private static XYZ CreateDesiredCenter(BoundingBoxXYZ box, DHBIMWATER.Core.Geometry.Point3D center, bool isXAxis)
    {
        var middleX = (box.Min.X + box.Max.X) / 2;
        var middleY = (box.Min.Y + box.Max.Y) / 2;
        return isXAxis
            ? new XYZ(middleX, UC.MmToFt(center.Y), UC.MmToFt(center.Z))
            : new XYZ(UC.MmToFt(center.X), middleY, UC.MmToFt(center.Z));
    }

    /// <summary>
    /// 독립기초는 로드 패밀리 인스턴스이므로 최상위 지오메트리는 Solid가 아니라 <see cref="GeometryInstance"/>이다.
    /// <see cref="GeometryInstance.GetInstanceGeometry()"/>로 한 단계 내려가야 실제 Solid/Face를 얻을 수 있다.
    /// </summary>
    private static IEnumerable<PlanarFace> EnumeratePlanarFaces(Element host)
    {
        var options = new Options { ComputeReferences = true };
        var geometry = host.get_Geometry(options);
        if (geometry == null) yield break;

        foreach (var planar in EnumeratePlanarFaces(geometry)) yield return planar;
    }

    private static IEnumerable<PlanarFace> EnumeratePlanarFaces(GeometryElement geometry)
    {
        foreach (var item in geometry)
        {
            switch (item)
            {
                case Solid solid when solid.Volume > 0:
                    foreach (Face face in solid.Faces)
                        if (face is PlanarFace planar) yield return planar;
                    break;
                case GeometryInstance instance:
                    // 인스턴스 좌표계 기준 지오메트리라야 면 Reference가 호스팅에 그대로 쓰인다.
                    foreach (var planar in EnumeratePlanarFaces(instance.GetInstanceGeometry())) yield return planar;
                    break;
            }
        }
    }

    /// <summary>면 탐색 실패 원인 파악용. 실제 검출된 평면 법선 목록을 문자열로 만든다.</summary>
    private static string DescribePlanarNormals(Element host)
    {
        var normals = EnumeratePlanarFaces(host)
            .Select(face => Format(face.FaceNormal.Normalize()))
            .Distinct()
            .ToList();
        return normals.Count == 0 ? "(없음 - Solid를 추출하지 못함)" : string.Join(", ", normals);
    }

    private static string Format(XYZ vector) => $"({vector.X:0.###}, {vector.Y:0.###}, {vector.Z:0.###})";
}
