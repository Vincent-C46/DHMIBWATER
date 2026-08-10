using DHBIMWATER.Application.DTOs.Gis;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Core.Gis;
using DHBIMWATER.Infrastructure.Helpers;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Modeling;

internal sealed class RevitAlignmentBeamPlacementRepo : IAlignmentBeamPlacementRepo
{
    private readonly Func<Document?> _doc;
    public RevitAlignmentBeamPlacementRepo(Func<Document?> doc) => _doc = doc;
    public int PlaceAlong(IReadOnlyList<PipeAlignment> alignments, string beamTypeName, string? levelName, double intervalM, bool alignTangent, AlignmentPlacementOrigin origin,
        IReadOnlyList<IReadOnlyList<VertexTrim>>? trims = null, string? diameterParameterName = null, string? kindParameterName = null)
    {
        var doc = _doc() ?? throw new InvalidOperationException("활성 Revit 문서가 없습니다.");
        var type = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralFraming).WhereElementIsElementType().Cast<FamilySymbol>().FirstOrDefault(x => x.Name == beamTypeName) ?? throw new InvalidOperationException($"빔 유형을 찾을 수 없습니다: {beamTypeName}");
        var level = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().FirstOrDefault(x => levelName is null || x.Name == levelName) ?? throw new InvalidOperationException("레벨을 찾을 수 없습니다.");
        if (!type.IsActive) type.Activate();
        // Revit 짧은 커브 허용치(약 0.00256ft ≈ 0.78mm)보다 짧은 세그먼트는 보 생성이 불가하므로 건너뛴다.
        var minLengthFt = doc.Application.ShortCurveTolerance;
        var basePoint = AlignmentPlacementMapper.GetProjectBasePoint(doc);
        var count = 0;
        // 매핑 대상 파라미터가 유형 매개변수면 인스턴스에서는 값을 쓸 수 없다 — 값 조합별로 유형을 복제해 그 유형에 설정한다.
        var diameterIsType = diameterParameterName is not null && type.LookupParameter(diameterParameterName) is not null;
        var kindIsType = kindParameterName is not null && type.LookupParameter(kindParameterName) is not null;
        var variantCache = new Dictionary<(double? Diameter, string? Kind), FamilySymbol>();
        // SamplePoints(점 배치)가 아니라 SampleSegments로 intervalM(6m)마다 끊어 시작/끝점을 잇는 선 기반 보를 생성한다.
        // alignTangent는 선 기반 보에서는 커브가 곧 방향이므로 사용하지 않는다(회전 불필요).
        // 곡관이 들어가는 정점에서는 그 몸통 자리(t, B형은 하류쪽 t+s)만큼 직관을 만들지 않는다.
        for (var index = 0; index < alignments.Count; index++)
        {
            var alignment = alignments[index];
            var vertexTrims = trims is not null && index < trims.Count ? trims[index] : null;
            var placementType = diameterIsType || kindIsType
                ? GetOrCreateVariantType(doc, type, variantCache, diameterIsType, kindIsType, diameterParameterName, kindParameterName, alignment.DiameterMm, alignment.PipeKind)
                : type;
            foreach (var segment in AlignmentIntervalSampler.SampleSegments(alignment.Vertices, intervalM, vertexTrims))
            {
                var start = ToXyz(segment.Start, alignment.DiameterMm, origin, basePoint);
                var end = ToXyz(segment.End, alignment.DiameterMm, origin, basePoint);
                if (start.DistanceTo(end) < minLengthFt) continue;
                var instance = doc.Create.NewFamilyInstance(Line.CreateBound(start, end), placementType, level, StructuralType.Beam);
                SuppressEndAdjustments(instance);
                SetAlignmentParameters(instance, diameterIsType ? null : diameterParameterName, kindIsType ? null : kindParameterName, alignment.DiameterMm, alignment.PipeKind);
                count++;
            }
        }
        return count;
    }

    // 같은 (직경, 관종) 조합은 유형 하나를 재사용한다. 조합별로 원본 유형을 복제해 유형 매개변수에 값을 굽는다.
    private static FamilySymbol GetOrCreateVariantType(Document doc, FamilySymbol baseType, Dictionary<(double? Diameter, string? Kind), FamilySymbol> cache,
        bool diameterIsType, bool kindIsType, string? diameterParameterName, string? kindParameterName, double diameterMm, string pipeKind)
    {
        var key = (diameterIsType ? diameterMm : (double?)null, kindIsType ? pipeKind : null);
        if (cache.TryGetValue(key, out var cached)) return cached;

        var suffixParts = new List<string>();
        if (diameterIsType) suffixParts.Add($"Ø{diameterMm:0.##}");
        if (kindIsType) suffixParts.Add(pipeKind);
        var typeName = $"{baseType.Name} - {string.Join(" ", suffixParts)}";

        var variant = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralFraming).WhereElementIsElementType().Cast<FamilySymbol>()
            .FirstOrDefault(x => string.Equals(x.Name, typeName, StringComparison.OrdinalIgnoreCase))
            ?? (FamilySymbol)baseType.Duplicate(typeName);
        if (diameterIsType) SetTypeParameter(variant, diameterParameterName!, diameterMm.ToString("0.##"), UC.MmToFt(diameterMm));
        if (kindIsType) SetTypeParameter(variant, kindParameterName!, pipeKind, null);
        if (!variant.IsActive) variant.Activate();

        cache[key] = variant;
        return variant;
    }

    private static void SetTypeParameter(FamilySymbol symbol, string name, string textValue, double? lengthValueFt)
    {
        var param = symbol.LookupParameter(name);
        if (param is not { IsReadOnly: false }) return;
        switch (param.StorageType)
        {
            case StorageType.Double when lengthValueFt is not null: param.Set(lengthValueFt.Value); break;
            case StorageType.String: param.Set(textValue); break;
        }
    }

    // 구조 프레이밍은 인접 보와 자동 조인되면서 끝단이 연장/컷백된다.
    // 그 결과 위치선 끝점은 절점에 있어도 형상(모양 핸들)이 절점을 넘어가고 다음 보 시작점도 밀려 보인다.
    // → 양단 조인을 해제하고 시작/끝 연장값을 0으로 고정해 형상을 위치선에 일치시킨다.
    private static void SuppressEndAdjustments(FamilyInstance instance)
    {
        StructuralFramingUtils.DisallowJoinAtEnd(instance, 0);
        StructuralFramingUtils.DisallowJoinAtEnd(instance, 1);
        SetZero(instance, BuiltInParameter.START_EXTENSION);
        SetZero(instance, BuiltInParameter.END_EXTENSION);
    }

    private static void SetZero(FamilyInstance instance, BuiltInParameter parameter)
    {
        var target = instance.get_Parameter(parameter);
        if (target is { IsReadOnly: false }) target.Set(0.0);
    }

    // 직경·관종 필드가 사용자 매핑(자동 매칭 또는 수동 선택)을 거쳐 지정된 경우에만 값을 쓴다. 매핑이 없으면(null) 건드리지 않는다.
    private static void SetAlignmentParameters(FamilyInstance instance, string? diameterParameterName, string? kindParameterName, double diameterMm, string pipeKind)
    {
        if (diameterParameterName is not null) SetParameter(instance, diameterParameterName, diameterMm.ToString("0.##"), UC.MmToFt(diameterMm));
        if (kindParameterName is not null) SetParameter(instance, kindParameterName, pipeKind, null);
    }

    // 매핑 대상 파라미터의 실제 자료형(길이·숫자·문자)은 패밀리마다 다를 수 있어 StorageType으로 분기한다.
    private static void SetParameter(FamilyInstance instance, string name, string textValue, double? lengthValueFt)
    {
        var param = instance.LookupParameter(name);
        if (param is not { IsReadOnly: false }) return;
        switch (param.StorageType)
        {
            case StorageType.Double when lengthValueFt is not null: param.Set(lengthValueFt.Value); break;
            case StorageType.String: param.Set(textValue); break;
        }
    }
    private static XYZ ToXyz(DHBIMWATER.Core.Geometry.Point3D point, double diameterMm, AlignmentPlacementOrigin origin, XYZ basePoint)
        => AlignmentPlacementMapper.ToXyz(point, diameterMm, origin.X, origin.Y, origin.ZDatum, basePoint);
}
