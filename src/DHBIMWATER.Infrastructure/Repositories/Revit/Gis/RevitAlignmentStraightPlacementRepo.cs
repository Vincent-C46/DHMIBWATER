using Autodesk.Revit.DB;
using System.IO;
using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Core.Gis;
using DHBIMWATER.Infrastructure.Helpers;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Gis;

/// <summary>
/// 관로 직관을 2점 가변(Adaptive Component) 패밀리로 배치한다.
/// 구조 프레이밍(빔) 배치를 대체한 것으로(2026-08-18), 곡관 5점 가변과 같은 표현 체계를 이룬다.
/// 유형 복제 없이 인스턴스 파라미터로 호칭지름·관종을 기록하며, 레벨과 접선 회전은 쓰지 않는다
/// (가변 컴포넌트는 Adaptive Point 좌표만으로 위치·방향이 결정된다).
/// </summary>
internal sealed class RevitAlignmentStraightPlacementRepo : IAlignmentStraightPlacementRepo
{
    /// <summary>세그먼트마다 Regenerate하면 수천 개 배치에서 매우 느려져, 이 개수마다 한 번씩만 호출한다.</summary>
    private const int RegenerateBatchSize = 200;

    private readonly Func<Document?> _doc;
    public RevitAlignmentStraightPlacementRepo(Func<Document?> doc) => _doc = doc;

    public AlignmentStraightPlacementResult PlaceAlong(IReadOnlyList<PipeAlignment> alignments, string straightFamilyTypeName, double intervalM, AlignmentPlacementOrigin origin,
        StraightPipeSpecTable specs, IReadOnlyList<IReadOnlyList<VertexTrim>>? trims = null, string? diameterParameterName = null, string? kindParameterName = null,
        string? outerDiameterParameterName = null, string? thicknessParameterName = null, PipeInfoParameterContext? info = null)
    {
        var doc = _doc() ?? throw new InvalidOperationException("활성 Revit 문서가 없습니다.");
        var symbol = ResolveSymbol(doc, straightFamilyTypeName);
        // Revit 짧은 커브 허용치(약 0.00256ft ≈ 0.78mm)보다 짧은 세그먼트는 배치 의미가 없어 건너뛴다.
        var minLengthFt = doc.Application.ShortCurveTolerance;
        var basePoint = AlignmentPlacementMapper.GetProjectBasePoint(doc);
        // 파라미터는 형상이 확정된 뒤(최종 Regenerate 이후) 한꺼번에 설정한다.
        var placed = new List<(FamilyInstance Instance, PipeAlignment Alignment, StraightPipeSpec? Spec, AlignmentSampleSegment Segment)>();
        var pending = 0;

        // SampleSegments로 intervalM(6m)마다 끊어 시작/끝점을 잇는다.
        // 곡관이 들어가는 정점에서는 그 몸통 자리(t, B형은 하류쪽 t+s)만큼 직관을 만들지 않는다.
        for (var index = 0; index < alignments.Count; index++)
        {
            var alignment = alignments[index];
            var spec = specs.Find(alignment.PipeKind, alignment.DiameterMm);
            var zOffsetM = origin.GetZOffsetM(alignment);
            var vertexTrims = trims is not null && index < trims.Count ? trims[index] : null;
            foreach (var segment in AlignmentIntervalSampler.SampleSegments(alignment.Vertices, intervalM, vertexTrims))
            {
                var start = ToXyz(segment.Start, zOffsetM, origin, basePoint);
                var end = ToXyz(segment.End, zOffsetM, origin, basePoint);
                if (start.DistanceTo(end) < minLengthFt) continue;

                var instance = AdaptiveComponentInstanceUtils.CreateAdaptiveComponentInstance(doc, symbol);
                var pointIds = AdaptiveComponentInstanceUtils.GetInstancePlacementPointElementRefIds(instance);
                if (pointIds.Count != 2)
                    throw new InvalidOperationException($"직관 패밀리 '{straightFamilyTypeName}'의 Adaptive Point가 2개가 아닙니다({pointIds.Count}개).");

                ((ReferencePoint)doc.GetElement(pointIds[0])).Position = start;
                ((ReferencePoint)doc.GetElement(pointIds[1])).Position = end;
                placed.Add((instance, alignment, spec, segment));
                if (++pending >= RegenerateBatchSize) { doc.Regenerate(); pending = 0; }
            }
        }
        if (pending > 0) doc.Regenerate();

        // 패밀리별로 한 번만 경고하면 충분하다 — 세그먼트마다 같은 누락 메시지가 반복되면 오히려 안 읽힌다.
        var missingParameters = new HashSet<string>();
        var writer = new PipeParameterWriter();
        foreach (var (instance, alignment, spec, segment) in placed)
        {
            if (diameterParameterName is not null) SetParameter(instance, diameterParameterName, alignment.DiameterMm.ToString("0.##"), UC.MmToFt(alignment.DiameterMm), missingParameters);
            if (kindParameterName is not null) SetParameter(instance, kindParameterName, alignment.PipeKind, null, missingParameters);
            if (spec is not null && outerDiameterParameterName is not null) SetParameter(instance, outerDiameterParameterName, spec.OuterDiameterMm.ToString("0.##"), UC.MmToFt(spec.OuterDiameterMm), missingParameters);
            if (spec is not null && thicknessParameterName is not null) SetParameter(instance, thicknessParameterName, spec.ThicknessMm.ToString("0.##"), UC.MmToFt(spec.ThicknessMm), missingParameters);
            if (info is not { Enabled: true }) continue;

            var lengthM = segment.Start.DistanceTo(segment.End);
            var runM = Math.Sqrt(Math.Pow(segment.End.X - segment.Start.X, 2) + Math.Pow(segment.End.Y - segment.Start.Y, 2));
            var riseM = segment.End.Z - segment.Start.Z;
            writer.Text(instance, PipeAlignmentParameters.Addin, PipeAlignmentParameters.AddinValue);
            writer.Text(instance, PipeAlignmentParameters.Part, PipeAlignmentParameters.StraightPartValue);
            writer.Text(instance, PipeAlignmentParameters.AlignmentId, AlignmentIdOf(alignment));
            writer.Text(instance, PipeAlignmentParameters.NominalDiameter, PipeAlignmentParameters.DiameterText(alignment.DiameterMm));
            writer.Text(instance, PipeAlignmentParameters.Material, MaterialLabelOf(alignment.PipeKind));
            writer.Text(instance, PipeAlignmentParameters.Grade, alignment.PipeKind);
            writer.LengthMm(instance, PipeAlignmentParameters.OuterDiameter, spec?.OuterDiameterMm);
            writer.LengthMm(instance, PipeAlignmentParameters.WallThickness, spec?.ThicknessMm);
            writer.Text(instance, PipeAlignmentParameters.JointType, info.JointType);
            writer.Text(instance, PipeAlignmentParameters.ElevationDatum, PipeAlignmentParameters.Label(info.ZDatum));
            writer.Text(instance, PipeAlignmentParameters.SourceFile, Path.GetFileName(alignment.SourceFile));
            writer.Text(instance, PipeAlignmentParameters.RecordNumber, alignment.RecordNumber);
            writer.LengthM(instance, PipeAlignmentParameters.Length, lengthM);
            writer.Number(instance, PipeAlignmentParameters.StartElevation, segment.Start.Z);
            writer.Number(instance, PipeAlignmentParameters.EndElevation, segment.End.Z);
            writer.Number(instance, PipeAlignmentParameters.Slope, runM > 1e-9 ? riseM / runM * 100d : 0d);
        }

        var warnings = new List<string>();
        if (missingParameters.Count > 0)
            warnings.Add($"직관 패밀리 '{straightFamilyTypeName}'에 {string.Join(", ", missingParameters.OrderBy(x => x))} 파라미터가 없거나 읽기전용이라 값을 설정하지 못했습니다.");
        if (writer.Missing.Count > 0)
            warnings.Add($"프로젝트 매개변수 {string.Join(", ", writer.Missing.OrderBy(x => x))}를 직관 인스턴스에 기록하지 못했습니다(바인딩 실패 또는 읽기전용).");
        return new AlignmentStraightPlacementResult(placed.Count, warnings);
    }

    /// <summary>"패밀리명 : 타입명" 문자열로 FamilySymbol을 찾는다. 표기는 곡관 목록(GetAdaptiveComponentTypeNames)과 같다.</summary>
    private static FamilySymbol ResolveSymbol(Document doc, string familyTypeName)
    {
        if (string.IsNullOrWhiteSpace(familyTypeName)) throw new InvalidOperationException("직관 패밀리를 선택하세요.");
        var separator = familyTypeName.LastIndexOf(" : ", StringComparison.Ordinal);
        if (separator <= 0 || separator >= familyTypeName.Length - 3)
            throw new InvalidOperationException($"직관 패밀리 표기가 '패밀리명 : 타입명' 형식이 아닙니다: {familyTypeName}");
        var familyName = familyTypeName[..separator];
        var typeName = familyTypeName[(separator + 3)..];

        var symbol = new FilteredElementCollector(doc)
            .OfClass(typeof(FamilySymbol))
            .Cast<FamilySymbol>()
            .FirstOrDefault(x => x.Family.Name == familyName && x.Name == typeName)
            ?? throw new InvalidOperationException($"직관 패밀리를 찾을 수 없습니다: {familyName} - {typeName}");
        if (!AdaptiveComponentFamilyUtils.IsAdaptiveComponentFamily(symbol.Family))
            throw new InvalidOperationException($"'{familyTypeName}'은 가변(Adaptive Component) 패밀리가 아닙니다.");
        if (!symbol.IsActive) symbol.Activate();
        return symbol;
    }

    // 매핑 대상 파라미터의 실제 자료형(길이·숫자·문자)은 패밀리마다 다를 수 있어 StorageType으로 분기한다.
    private static void SetParameter(FamilyInstance instance, string name, string textValue, double? lengthValueFt, HashSet<string> missingParameters)
    {
        var param = instance.LookupParameter(name);
        if (param is not { IsReadOnly: false }) { missingParameters.Add(name); return; }
        switch (param.StorageType)
        {
            case StorageType.Double when lengthValueFt is not null: param.Set(lengthValueFt.Value); break;
            case StorageType.String: param.Set(textValue); break;
        }
    }

    private static XYZ ToXyz(DHBIMWATER.Core.Geometry.Point3D point, double zOffsetM, AlignmentPlacementOrigin origin, XYZ basePoint)
        => AlignmentPlacementMapper.ToXyz(point, zOffsetM, origin.X, origin.Y, basePoint);

    private static string AlignmentIdOf(PipeAlignment alignment) => $"{Path.GetFileNameWithoutExtension(alignment.SourceFile)}#{alignment.RecordNumber}";
    private static string MaterialLabelOf(string pipeKind) => PipeKindCatalog.MaterialOf(pipeKind) is { } material ? PipeAlignmentParameters.Label(material) : string.Empty;
}
