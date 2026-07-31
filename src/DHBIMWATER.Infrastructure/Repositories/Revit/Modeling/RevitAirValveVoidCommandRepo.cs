using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.DTOs.Revit.ValveRoom;
using DHBIMWATER.Application.Interfaces;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Modeling;

/// <summary><c>void_면기반</c>을 공기밸브실 독립기초의 X/Y 측면에 호스팅한다.</summary>
public sealed class RevitAirValveVoidCommandRepo : IAirValveVoidCommandRepo
{
    private const string FamilyName = "void_면기반_2025";
    private const string DepthParameterName = "Depth";

    /// <summary>Void가 기초 반대편을 빠져나가도록 관통 길이에 더하는 여유(mm).</summary>
    private const double PenetrationMarginMm = 100;

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
        var desiredCenter = CreateDesiredCenter(box, definition.PipeCenter);
        var host = FindPlanarFace(foundation, targetNormal, desiredCenter)
            ?? throw new InvalidOperationException(
                $"독립기초에서 {definition.Axis}축 Void를 위한 시작면(법선 {Format(targetNormal)}, 중심 {Format(desiredCenter)})을 찾을 수 없습니다. " +
                $"검출된 평면 법선: {DescribePlanarNormals(foundation)}");

        // 절단 판정 기준값. Void를 배치하기 전의 기초 체적이어야 하므로 배치보다 먼저 읽는다.
        var volumeBeforePlacement = ComputeCutSolidVolume(foundation);

        // 면 선택은 심볼 좌표계 형상으로 하지만(Face.Reference가 거기에만 있다), 이 오버로드의 배치점·기준 방향은
        // 모델 좌표다. 심볼 좌표를 넘기면 인스턴스가 회전·이동량만큼 평면에서 어긋난 위치에 떨어진다.
        var instance = doc.Create.NewFamilyInstance(host.Face.Reference, host.ModelPoint, XYZ.BasisZ, symbol);
        var radius = instance.LookupParameter("r");
        if (radius == null || radius.IsReadOnly) throw new InvalidOperationException($"면기반 Void 패밀리 '{FamilyName}'에서 읽기 가능한 인스턴스 매개변수 'r'을 찾을 수 없습니다.");
        radius.Set(UC.MmToFt(definition.Radius));

        // 관통 길이는 기초의 해당 축 방향 전체 길이 + 여유. 시작면에서 기초 안쪽으로 돌출하므로
        // 여유분만큼 반대편으로 빠져나간다. 기초마다 길이가 다르므로 유형이 아니라 인스턴스 매개변수여야 한다.
        var hostLength = isXAxis ? box.Max.X - box.Min.X : box.Max.Y - box.Min.Y;
        var depth = instance.LookupParameter(DepthParameterName);
        if (depth == null || depth.IsReadOnly)
            throw new InvalidOperationException(
                $"면기반 Void 패밀리 '{FamilyName}'에서 쓰기 가능한 인스턴스 매개변수 '{DepthParameterName}'을(를) 찾을 수 없습니다. " +
                $"유형 매개변수로 되어 있으면 인스턴스 매개변수로 변경해야 합니다.");
        depth.Set(hostLength + UC.MmToFt(PenetrationMarginMm));
        doc.Regenerate();

        AlignToTarget(doc, instance, host.ModelPoint, host.ModelNormal);
        // 보정 이동 결과를 절단 판정 전에 형상에 반영시킨다.
        doc.Regenerate();

        EnsureVoidCut(doc, foundation, instance, volumeBeforePlacement);
        instance.LookupParameter("DH_Addin")?.Set("DHBIMWATER");
        instance.LookupParameter("DH_Category")?.Set("개구부");
        instance.LookupParameter("DH_Part")?.Set("본관 관통 Void");
    }

    /// <summary>법선 일치 판정 허용오차. 기초 회전(-90°) 시 발생하는 부동소수 오차를 흡수한다.</summary>
    private const double NormalTolerance = 0.999;

    /// <summary>배치 결과와 목표점의 차이를 보정할 기준 거리(0.1mm).</summary>
    private static readonly double AlignTolerance = UC.MmToFt(0.1);

    /// <summary>선택한 호스트 면과, 그 면을 모델 좌표로 옮기는 변환·모델 좌표 배치점·모델 좌표 법선 묶음.</summary>
    private readonly record struct HostFace(PlanarFace Face, Transform Transform, XYZ ModelPoint, XYZ ModelNormal);

    /// <summary>
    /// 배치 결과가 의도한 관통 중심과 어긋나면 면 안에서 보정한다.
    /// 호스트 면 참조의 좌표계 해석에만 의존하지 않기 위한 안전장치다.
    /// </summary>
    private static void AlignToTarget(Document doc, FamilyInstance instance, XYZ targetModelPoint, XYZ modelNormal)
    {
        if (instance.Location is not LocationPoint location) return;
        var delta = targetModelPoint - location.Point;
        // 호스트 면에서 떨어지지 않도록 법선 성분은 버리고 면 방향 성분만 이동한다.
        var inPlane = delta - modelNormal.Multiply(delta.DotProduct(modelNormal));
        if (inPlane.GetLength() < AlignTolerance) return;
        ElementTransformUtils.MoveElement(doc, instance.Id, inPlane);
    }

    /// <summary>
    /// 관통 중심이 실제 Face 경계 안에 들어가는 시작면만 선택한다.
    /// BoundingBox 좌표는 회전한 로드 패밀리의 실제 면 위치를 대변하지 않으므로 배치점으로 쓰지 않는다.
    /// 법선 판정은 회전이 반영된 모델 좌표로, 투영·배치점은 Face.Reference와 짝이 맞는 심볼 좌표로 수행한다.
    /// </summary>
    private static HostFace? FindPlanarFace(Element host, XYZ targetNormal, XYZ desiredCenter)
    {
        foreach (var (planar, transform) in EnumeratePlanarFaces(host))
        {
            var modelNormal = transform.OfVector(planar.FaceNormal).Normalize();
            if (modelNormal.DotProduct(targetNormal) <= NormalTolerance) continue;
            // 면 형상이 심볼 좌표계이므로 투영할 점도 심볼 좌표로 변환한 뒤 투영하고, 결과는 모델 좌표로 되돌린다.
            var projection = planar.Project(transform.Inverse.OfPoint(desiredCenter));
            if (projection != null && planar.IsInside(projection.UVPoint))
                return new HostFace(planar, transform, transform.OfPoint(projection.XYZPoint), modelNormal);
        }
        return null;
    }

    /// <summary>
    /// 관통 중심 목표점(모델 좌표). 평면 위치는 기초 옆면의 이등분점이므로 X·Y 모두 기초 BoundingBox 중심을 쓰고,
    /// 표고만 본관 중심 깊이를 따른다. 시작면 법선 좌표는 어차피 Face 투영으로 확정되므로 중심값이면 충분하다.
    /// </summary>
    private static XYZ CreateDesiredCenter(BoundingBoxXYZ box, DHBIMWATER.Core.Geometry.Point3D center) =>
        new((box.Min.X + box.Max.X) / 2, (box.Min.Y + box.Max.Y) / 2, UC.MmToFt(center.Z));

    /// <summary>
    /// 독립기초는 로드 패밀리 인스턴스이므로 최상위 지오메트리는 Solid가 아니라 <see cref="GeometryInstance"/>이다.
    /// 호스팅에 쓸 수 있는 <c>Face.Reference</c>는 <see cref="GeometryInstance.GetSymbolGeometry()"/> 쪽에만 있으므로
    /// 심볼 좌표계 형상을 순회하되, 모델 좌표로 되돌릴 <see cref="Transform"/>을 함께 넘긴다.
    /// 중첩 인스턴스에서도 회전이 누락되지 않도록 변환을 누적한다.
    /// </summary>
    private static IEnumerable<(PlanarFace Face, Transform Transform)> EnumeratePlanarFaces(Element host)
    {
        var options = new Options { ComputeReferences = true };
        var geometry = host.get_Geometry(options);
        return geometry == null
            ? Enumerable.Empty<(PlanarFace, Transform)>()
            : EnumeratePlanarFaces(geometry, Transform.Identity);
    }

    private static IEnumerable<(PlanarFace Face, Transform Transform)> EnumeratePlanarFaces(GeometryElement geometry, Transform transform)
    {
        foreach (var item in geometry)
        {
            switch (item)
            {
                case Solid solid when solid.Volume > 0:
                    foreach (Face face in solid.Faces)
                        if (face is PlanarFace planar) yield return (planar, transform);
                    break;
                case GeometryInstance instance:
                    foreach (var nested in EnumeratePlanarFaces(instance.GetSymbolGeometry(), transform.Multiply(instance.Transform)))
                        yield return nested;
                    break;
            }
        }
    }

    /// <summary>절단 성공 판정에 쓰는 최소 체적 감소량(ft³). 형상 계산 오차보다 크고 실제 관통 체적보다 훨씬 작다.</summary>
    private const double CutVolumeTolerance = 1e-4;

    /// <summary>
    /// 기초가 실제로 절단됐는지 체적 감소로 확인하고, 절단되지 않았을 때만 절단 관계를 명시적으로 만든다.
    /// 면기반 Void는 호스트 면에 배치되는 순간 자동으로 호스트를 절단하는데, 이 자동 절단은
    /// <see cref="InstanceVoidCutUtils.InstanceVoidCutExists"/>로 조회되지 않는다. 그래서 존재 여부만 보고
    /// <see cref="InstanceVoidCutUtils.AddInstanceVoidCut"/>을 호출하면 이미 붙은(attached) Void라는 이유로
    /// "unattached void" 예외가 나면서, 정작 형상은 정상인 경우까지 실패로 처리된다.
    /// <para>
    /// 절단 실패는 예외로 올리지 않는다. 밸브실 생성 트랜잭션 전체가 롤백되면 절단 하나 때문에 정상 생성된
    /// 기초·벽체·슬래브까지 사라지기 때문이다. Void 인스턴스는 배치된 채로 남으므로 수동 절단이 가능하다.
    /// </para>
    /// </summary>
    private static void EnsureVoidCut(Document doc, Element host, FamilyInstance voidInstance, double volumeBeforePlacement)
    {
        // 자동 절단 또는 기존 절단 관계로 이미 잘렸으면 더 할 일이 없다.
        if (volumeBeforePlacement - ComputeCutSolidVolume(host) > CutVolumeTolerance) return;
        if (InstanceVoidCutUtils.InstanceVoidCutExists(host, voidInstance)) return;
        if (!InstanceVoidCutUtils.CanBeCutWithVoid(host))
        {
            WarnCutFailed("독립기초가 Void 인스턴스로 절단할 수 없는 요소입니다. 기초 패밀리 유형을 확인해야 합니다.");
            return;
        }
        try
        {
            InstanceVoidCutUtils.AddInstanceVoidCut(doc, host, voidInstance);
        }
        catch (Exception exception)
        {
            // 자동 절단도 안 됐고 명시적 절단도 거부된 상태. 배치점·관통 길이는 앞 단계에서 확정했으므로
            // 남는 원인은 RFA 쪽 Void 설정이다.
            WarnCutFailed(
                $"패밀리 '{FamilyName}'의 Void 폼이 패밀리 내부 솔리드에 이미 붙어 있거나(Cut Geometry 적용됨), " +
                $"패밀리 유형 속성의 '로드할 때 보이드로 절단(Cut with Voids When Loaded)'이 꺼져 있는지 확인해야 합니다.\n\n({exception.Message})");
            return;
        }
        doc.Regenerate();
        if (volumeBeforePlacement - ComputeCutSolidVolume(host) <= CutVolumeTolerance)
            WarnCutFailed(
                "절단 관계는 만들어졌지만 독립기초의 체적이 줄지 않았습니다. Void 형상이 기초와 교차하지 않습니다. " +
                "본관 중심 깊이와 직경이 기초 두께 안에 들어오는지 확인해야 합니다.");
    }

    /// <summary>절단 실패를 알리되 생성 작업은 계속한다. 모델과 Void 인스턴스는 그대로 남는다.</summary>
    // TODO: 다른 Revit Repo와 동일하게 TaskDialog를 직접 쓴다. 추후 IDialogService 주입으로 통일하면 좋다.
    private static void WarnCutFailed(string reason) =>
        TaskDialog.Show(
            "Void 절단 실패",
            $"Void가 독립기초를 절단하지 못했습니다. {reason}\n\n" +
            "모델 생성은 계속 진행합니다. Void 인스턴스는 배치된 상태로 남아 있으므로 Revit에서 직접 절단할 수 있습니다.");

    /// <summary>
    /// 절단이 반영된 기초 솔리드의 총 체적(ft³). 면 탐색과 달리 <see cref="GeometryInstance.GetInstanceGeometry()"/>를 써야 한다.
    /// 심볼 형상(<see cref="GeometryInstance.GetSymbolGeometry()"/>)은 인스턴스별 절단이 반영되지 않은 원본이라 체적이 변하지 않는다.
    /// </summary>
    private static double ComputeCutSolidVolume(Element host)
    {
        var geometry = host.get_Geometry(new Options { DetailLevel = ViewDetailLevel.Fine });
        return geometry == null ? 0 : SumSolidVolume(geometry);
    }

    private static double SumSolidVolume(GeometryElement geometry)
    {
        var total = 0d;
        foreach (var item in geometry)
        {
            total += item switch
            {
                Solid solid => solid.Volume,
                GeometryInstance instance => SumSolidVolume(instance.GetInstanceGeometry()),
                _ => 0
            };
        }
        return total;
    }

    /// <summary>면 탐색 실패 원인 파악용. 실제 검출된 평면 법선 목록을 모델 좌표로 만든다.</summary>
    private static string DescribePlanarNormals(Element host)
    {
        var normals = EnumeratePlanarFaces(host)
            .Select(item => Format(item.Transform.OfVector(item.Face.FaceNormal).Normalize()))
            .Distinct()
            .ToList();
        return normals.Count == 0 ? "(없음 - Solid를 추출하지 못함)" : string.Join(", ", normals);
    }

    private static string Format(XYZ vector) => $"({vector.X:0.###}, {vector.Y:0.###}, {vector.Z:0.###})";
}
