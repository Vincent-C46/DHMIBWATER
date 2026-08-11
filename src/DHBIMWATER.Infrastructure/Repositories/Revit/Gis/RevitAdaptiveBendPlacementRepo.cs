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

    public int Place(IReadOnlyList<AdaptiveBendPlacementPlan> plans, AlignmentPlacementOrigin origin)
    {
        if (plans.Count == 0) return 0;
        var doc = _doc() ?? throw new InvalidOperationException("활성 Revit 문서가 없습니다.");
        var basePoint = AlignmentPlacementMapper.GetProjectBasePoint(doc);
        var symbols = new Dictionary<(string Family, string Type), FamilySymbol>();
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

            SetLengthParameter(instance, OuterDiameterParameter, UC.MmToFt(plan.OuterDiameterMm));
            SetLengthParameter(instance, WallThicknessParameter, UC.MmToFt(plan.WallThicknessMm));
            for (var i = 0; i < 5; i++)
            {
                SetAngleParameter(instance, $"{RotationXyParameterPrefix}{i + 1}", plan.RotXYDeg[i]);
                SetAngleParameter(instance, $"{RotationXzParameterPrefix}{i + 1}", plan.RotXZDeg[i]);
            }
            count++;
        }
        return count;
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

    private static void SetLengthParameter(FamilyInstance instance, string name, double valueFt)
    {
        var param = instance.LookupParameter(name);
        if (param is { IsReadOnly: false }) param.Set(valueFt);
    }

    private static void SetAngleParameter(FamilyInstance instance, string name, double degrees)
    {
        var param = instance.LookupParameter(name);
        if (param is { IsReadOnly: false }) param.Set(UC.DegToRad(degrees));
    }
}
