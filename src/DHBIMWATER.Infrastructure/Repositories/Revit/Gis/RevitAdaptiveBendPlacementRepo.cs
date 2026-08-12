using Autodesk.Revit.DB;
using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Core.Gis;
using DHBIMWATER.Infrastructure.Helpers;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Gis;

/// <summary>
/// 5점 가변(Adaptive Component) 곡관 패밀리를 배치한다. Beam 모드 전용이라 Connector 연결은 하지 않는다.
/// </summary>
internal sealed class RevitAdaptiveBendPlacementRepo : IAdaptiveBendPlacementRepo
{
    private const string OuterDiameterParameter = "OD";
    private const string WallThicknessParameter = "thk";
    private const string RotationXyParameterPrefix = "rot_XY_";
    private const string RotationXzParameterPrefix = "rot_XZ_";

    private readonly Func<Document?> _doc;
    public RevitAdaptiveBendPlacementRepo(Func<Document?> doc) => _doc = doc;

    public AdaptiveBendPlacementResult Place(IReadOnlyList<AdaptiveBendPlacementPlan> plans, AlignmentPlacementOrigin origin)
    {
        if (plans.Count == 0) return new AdaptiveBendPlacementResult(0, Array.Empty<string>());
        var doc = _doc() ?? throw new InvalidOperationException("활성 Revit 문서가 없습니다.");
        var basePoint = AlignmentPlacementMapper.GetProjectBasePoint(doc);
        var symbols = new Dictionary<(string Family, string Type), FamilySymbol>();
        // 패밀리별로 한 번만 경고하면 충분하다 — 절점마다 같은 파라미터 누락 메시지가 반복되면 오히려 안 읽힌다.
        var missingParameters = new HashSet<(string Family, string Type, string Parameter)>();
        var count = 0;

        foreach (var plan in plans)
        {
            var symbol = ResolveSymbol(doc, symbols, plan.FamilyName, plan.TypeName);
            var instance = AdaptiveComponentInstanceUtils.CreateAdaptiveComponentInstance(doc, symbol);
            var pointIds = AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(instance);
            if (pointIds.Count != 5)
                throw new InvalidOperationException($"곡관 패밀리 '{plan.FamilyName}:{plan.TypeName}'의 Adaptive Point가 5개가 아닙니다({pointIds.Count}개).");

            var points = new[] { plan.Points.Start, plan.Points.ArcStart, plan.Points.ArcMid, plan.Points.ArcEnd, plan.Points.End };
            for (var i = 0; i < 5; i++)
            {
                var refPoint = (ReferencePoint)doc.GetElement(pointIds[i]);
                // ZDatum 보정(관 크라운/인버트)은 인접 직관과 같은 OD를 넣어야 Z가 어긋나지 않는다.
                refPoint.Position = AlignmentPlacementMapper.ToXyz(points[i], plan.OuterDiameterMm, origin.X, origin.Y, origin.ZDatum, basePoint);
            }
            doc.Regenerate();

            SetLengthParameter(instance, plan, OuterDiameterParameter, UC.MmToFt(plan.OuterDiameterMm), missingParameters);
            SetLengthParameter(instance, plan, WallThicknessParameter, UC.MmToFt(plan.WallThicknessMm), missingParameters);
            for (var i = 0; i < 5; i++)
            {
                SetAngleParameter(instance, plan, $"{RotationXyParameterPrefix}{i + 1}", plan.RotXYDeg[i], missingParameters);
                SetAngleParameter(instance, plan, $"{RotationXzParameterPrefix}{i + 1}", plan.RotXZDeg[i], missingParameters);
            }
            count++;
        }

        var warnings = missingParameters
            .GroupBy(x => (x.Family, x.Type))
            .Select(g => $"곡관 패밀리 '{g.Key.Family}:{g.Key.Type}'에 {string.Join(", ", g.Select(x => x.Parameter))} 파라미터가 없거나 읽기전용이라 값을 설정하지 못했습니다.")
            .ToList();
        return new AdaptiveBendPlacementResult(count, warnings);
    }

    public IReadOnlyList<(string FamilyName, string TypeName)> FindMissingSymbols(IEnumerable<(string FamilyName, string TypeName)> pairs)
    {
        var doc = _doc();
        var distinct = pairs.Distinct().ToList();
        if (doc is null) return distinct;

        var loaded = new FilteredElementCollector(doc)
            .OfClass(typeof(FamilySymbol))
            .Cast<FamilySymbol>()
            .Select(x => (x.Family.Name, x.Name))
            .ToHashSet();
        return distinct.Where(x => !loaded.Contains(x)).ToList();
    }

    // 같은 (패밀리, 타입) 조합은 FilteredElementCollector 재조회 없이 캐시에서 재사용한다.
    private static FamilySymbol ResolveSymbol(Document doc, Dictionary<(string, string), FamilySymbol> cache, string familyName, string typeName)
    {
        var key = (familyName, typeName);
        if (cache.TryGetValue(key, out var cached)) return cached;

        var symbol = new FilteredElementCollector(doc)
            .OfClass(typeof(FamilySymbol))
            .Cast<FamilySymbol>()
            .FirstOrDefault(x => x.Family.Name == familyName && x.Name == typeName)
            ?? throw new InvalidOperationException($"곡관 패밀리를 찾을 수 없습니다: {familyName} - {typeName}");
        if (!symbol.IsActive) symbol.Activate();

        cache[key] = symbol;
        return symbol;
    }

    private static void SetLengthParameter(FamilyInstance instance, AdaptiveBendPlacementPlan plan, string name, double valueFt, HashSet<(string, string, string)> missingParameters)
    {
        var param = instance.LookupParameter(name);
        if (param is { IsReadOnly: false }) param.Set(valueFt);
        else missingParameters.Add((plan.FamilyName, plan.TypeName, name));
    }

    private static void SetAngleParameter(FamilyInstance instance, AdaptiveBendPlacementPlan plan, string name, double degrees, HashSet<(string, string, string)> missingParameters)
    {
        var param = instance.LookupParameter(name);
        if (param is { IsReadOnly: false }) param.Set(UC.DegToRad(degrees));
        else missingParameters.Add((plan.FamilyName, plan.TypeName, name));
    }
}
