using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Application.Gis;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Core.Gis;
using DHBIMWATER.Core.Parameters;

namespace DHBIMWATER.Application.UseCases.Gis;

public sealed class ModelPipeAlignmentUseCase
{
    private readonly ITransactionContext _transaction;
    private readonly AlignmentSourceLoader _loader;
    private readonly IPipeAlignmentCommandRepo _alignmentRepo;
    private readonly IAlignmentStraightPlacementRepo _straightRepo;
    private readonly IAlignmentPipePlacementRepo _pipeRepo;
    private readonly IProjectLocationCommandRepo _projectLocationRepo;
    private readonly ISharedParameterRepository _sharedParameterRepo;
    private readonly BendSettingsProvider _bendSettings;
    private readonly IAdaptiveBendPlacementRepo _adaptiveBendRepo;

    public ModelPipeAlignmentUseCase(ITransactionContext transaction, AlignmentSourceLoader loader, IPipeAlignmentCommandRepo alignmentRepo,
        IAlignmentStraightPlacementRepo straightRepo, IAlignmentPipePlacementRepo pipeRepo, IProjectLocationCommandRepo projectLocationRepo,
        ISharedParameterRepository sharedParameterRepo, BendSettingsProvider bendSettings, IAdaptiveBendPlacementRepo adaptiveBendRepo)
    { _transaction = transaction; _loader = loader; _alignmentRepo = alignmentRepo; _straightRepo = straightRepo; _pipeRepo = pipeRepo; _projectLocationRepo = projectLocationRepo; _sharedParameterRepo = sharedParameterRepo; _bendSettings = bendSettings; _adaptiveBendRepo = adaptiveBendRepo; }

    public PipeAlignmentModelingResult Execute(PipeAlignmentModelingRequest request)
    {
        if (request.OutputMode is PipeAlignmentOutputMode.Adaptive or PipeAlignmentOutputMode.PipingSystem && request.IntervalMm <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.IntervalMm));
        var loaded = _loader.Load(request.Files);
        var reference = request.ReferenceX == 0 && request.ReferenceY == 0
            ? AlignmentReferencePoint.FromFirstVertex(loaded.Alignments)
            : ((double X, double Y)?)(request.ReferenceX, request.ReferenceY);
        var settingsResolution = request.CurrentBendSettings is null
            ? _bendSettings.Load()
            : new BendSettingsResolution(request.CurrentBendSettings, BendSettingsSource.Project);
        var (settings, settingsSource) = settingsResolution;
        var origin = new AlignmentPlacementOrigin(reference?.X ?? 0, reference?.Y ?? 0, request.ZDatum, settings.StraightPipes);
        var infoContext = new PipeInfoParameterContext(settings.ActiveJointType, settings.ApplicationMode, request.ZDatum, request.Form);
        var elevationWarnings = GetElevationWarnings(loaded.Alignments, request, settings.StraightPipes);
        // 곡관 자리 계산은 Adaptive 모드에만 적용한다(사용자 결정 2026-08-07, 2026-08-18 명칭 변경).
        // PipingSystem은 Revit이 NewElbowFitting으로 부속을 자동 생성해 이중이 되고,
        // DirectShape는 폴리선 원형을 그대로 형상화하는 경로다.
        var (bendPlan, bendWarnings) = request.OutputMode == PipeAlignmentOutputMode.Adaptive
            ? PlanBends(loaded.Alignments, request, settings, settingsSource == BendSettingsSource.BuiltInDefault)
            : (null, Array.Empty<string>());

        // 트랜잭션을 열기 전에 카탈로그가 가리키는 곡관 패밀리가 실제로 이 문서에 로드돼 있는지 확인한다.
        // 그렇지 않으면 직관 배치까지 다 끝낸 뒤 곡관 배치 단계에서야 실패해 전체가 롤백된다.
        if (bendPlan is not null)
        {
            EnsureBendConfiguration(bendPlan, request.BendFamilyName);
            EnsureBendFamiliesLoaded(bendPlan, request.BendFamilyName);
        }

        using (_transaction)
        {
            try
            {
                _transaction.Begin(request.OutputMode == PipeAlignmentOutputMode.DirectShape ? "Import Pipe Alignment" : "선형 패밀리 배치");
                if (request.OutputMode == PipeAlignmentOutputMode.DirectShape) _sharedParameterRepo.EnsureParameters(GetAlignmentParameterDefinitions());
                else if (request.OutputMode == PipeAlignmentOutputMode.Adaptive) _sharedParameterRepo.EnsureParameters(PipeAlignmentParameters.Definitions);
                if (request.ApplySharedCoordinates) _projectLocationRepo.SetInternalOriginSharedPosition(origin.X, origin.Y, 0);
                var (count, skipped, repoWarnings) = request.OutputMode switch
                {
                    PipeAlignmentOutputMode.DirectShape => ToDirectShape(loaded.Alignments, origin),
                    PipeAlignmentOutputMode.Adaptive => ToStraight(_straightRepo.PlaceAlong(loaded.Alignments, Require(request.StraightFamilyTypeName, "직관 패밀리"), request.IntervalMm / 1000d, origin, settings.StraightPipes, bendPlan?.Plans.Select(x => x.Trims).ToList(), request.StraightDiameterParameterName, request.StraightKindParameterName, request.StraightOuterDiameterParameterName, request.StraightThicknessParameterName, infoContext), bendWarnings),
                    PipeAlignmentOutputMode.PipingSystem => (_pipeRepo.PlaceAlong(loaded.Alignments, Require(request.PipingSystemTypeName, "파이프 시스템 유형"), Require(request.PipeTypeName, "PipeType"), request.LevelName, request.IntervalMm / 1000d, origin), 0, (IReadOnlyList<string>)Array.Empty<string>()),
                    _ => throw new ArgumentOutOfRangeException(nameof(request.OutputMode))
                };
                var bendResult = request.OutputMode == PipeAlignmentOutputMode.Adaptive && bendPlan is not null
                    ? PlaceBendFittings(bendPlan, loaded.Alignments, origin, request, infoContext)
                    : new AdaptiveBendPlacementResult(0, Array.Empty<string>());
                _transaction.Commit();
                return new PipeAlignmentModelingResult(request.OutputMode, count, skipped, loaded.Warnings.Concat(elevationWarnings).Concat(repoWarnings).Concat(bendResult.Warnings).ToList(), bendResult.Count);
            }
            catch { _transaction.Rollback(); throw; }
        }
    }

    private static void EnsureBendConfiguration(BendTrimPlan bendPlan, string? familyName)
    {
        if (bendPlan.Placements.Count == 0) return;
        if (string.IsNullOrWhiteSpace(familyName))
            throw new InvalidOperationException("배치할 곡관이 있습니다. 5점 가변 곡관 패밀리를 선택하세요.");

        var missingTypes = bendPlan.Placements
            .Where(x => string.IsNullOrWhiteSpace(x.TypeName))
            .Select(x => $"{x.PipeKind}/DN{x.DiameterMm:0.##}/{x.AngleDeg:0.##}°")
            .Distinct()
            .ToList();
        if (missingTypes.Count > 0)
            throw new InvalidOperationException($"다음 곡관 규격의 유형을 관·곡관 규격표에서 선택하세요: {string.Join(", ", missingTypes)}");
    }

    private void EnsureBendFamiliesLoaded(BendTrimPlan bendPlan, string? familyName)
    {
        var pairs = bendPlan.Placements
            .Where(x => !string.IsNullOrWhiteSpace(familyName) && x.TypeName is not null)
            .Select(x => (familyName!, x.TypeName!))
            .ToList();
        if (pairs.Count == 0) return;

        var missing = _adaptiveBendRepo.FindMissingSymbols(pairs);
        if (missing.Count > 0)
            throw new InvalidOperationException($"다음 곡관 패밀리·타입을 문서에서 찾을 수 없습니다(로드 필요): {string.Join(", ", missing.Select(x => $"{x.FamilyName}:{x.TypeName}"))}");
    }

    /// <summary>
    /// 절점 곡관 판정을 폴리선 정점 차감량과 5점 가변 곡관 배치 계획으로 환산한다.
    /// </summary>
    private (BendTrimPlan? Plan, IReadOnlyList<string> Warnings) PlanBends(IReadOnlyList<PipeAlignment> alignments, PipeAlignmentModelingRequest request, BendSettings settings, bool usesDefault)
    {
        if (request.SnapToleranceMm <= 0) throw new ArgumentOutOfRangeException(nameof(request.SnapToleranceMm));

        var warnings = new List<string>();
        if (usesDefault) warnings.Add("저장된 관로 규격 설정이 없어 내장 기본값으로 판정했습니다.");
        if (settings.Fittings.IsPlaceholder) warnings.Add("곡관 치수가 실제 규격이 아니라 임시값입니다. 배치된 곡관의 길이·곡률반경은 검토용으로만 쓰세요.");

        var snapTolerance = request.SnapToleranceMm / 1000d;
        var graph = PipeNetworkBuilder.Build(alignments, snapTolerance);
        var nodes = PipeNetworkClassifier.Classify(graph);
        var resolutions = BendResolver.ResolveAll(nodes, settings, request.Form);
        var plan = BendTrimPlanner.Plan(alignments, graph, resolutions, snapTolerance);
        warnings.AddRange(plan.Warnings);

        var skipped = resolutions.Count(x => x.Kind != BendResolutionKind.None && !x.HasFittingSize);
        if (skipped > 0)
            warnings.Add($"곡관 치수가 없어 배치하지 못한 절점 {skipped}곳은 직관을 절점까지 붙였습니다.");

        var missingFamily = plan.Placements.Count(x => string.IsNullOrWhiteSpace(request.BendFamilyName) || x.TypeName is null);
        if (missingFamily > 0)
            warnings.Add($"곡관 패밀리·타입이 설정되지 않아 실물 배치를 생략한 절점 {missingFamily}곳은 자리만 비웠습니다.");
        var exceeded = resolutions.Where(x => !x.IsAcceptable).Select(x => x.NodeId).ToList();
        if (exceeded.Count > 0)
            warnings.Add($"허용굴곡 초과 절점 {exceeded.Count}곳: {string.Join(", ", exceeded)}. 배치 가능한 곡관은 3D 뷰에서 적색 표시합니다.");

        return (plan, warnings);
    }

    /// <summary>카탈로그에 패밀리·타입이 등록된 절점만 골라 5점 가변 곡관을 배치한다.</summary>
    private AdaptiveBendPlacementResult PlaceBendFittings(BendTrimPlan plan, IReadOnlyList<PipeAlignment> alignments, AlignmentPlacementOrigin origin, PipeAlignmentModelingRequest request, PipeInfoParameterContext infoContext)
    {
        var placeable = plan.Placements.Where(x => !string.IsNullOrWhiteSpace(request.BendFamilyName) && x.TypeName is not null).ToList();
        if (placeable.Count == 0) return new AdaptiveBendPlacementResult(0, Array.Empty<string>());

        var bendPlans = placeable.Select(x =>
        {
            var alignment = alignments[x.AlignmentIndex];
            var spec = origin.StraightPipeSpecs.Find(x.PipeKind, x.DiameterMm);
            return new AdaptiveBendPlacementPlan(
                x.NodeId, request.BendFamilyName!, x.TypeName!, x.Points, x.DiameterMm, x.WallThicknessMm,
                origin.GetZOffsetM(x.PipeKind, x.DiameterMm), request.BendDiameterParameterName, request.BendWallThicknessParameterName, x.IsAcceptable,
                x.RotXYDeg, x.RotXZDeg, x.PipeKind, alignment.SourceFile, alignment.RecordNumber, spec?.OuterDiameterMm,
                x.DeflectionDeg, x.AngleDeg, x.EffectiveAllowableDeg, x.ResidualDeg, x.CenterlineRadiusMm, x.LayingLengthMm);
        }).ToList();
        return _adaptiveBendRepo.Place(bendPlans, origin, infoContext);
    }

    private (int Count, int Skipped, IReadOnlyList<string> Warnings) ToDirectShape(IReadOnlyList<PipeAlignment> alignments, AlignmentPlacementOrigin origin)
    { var result = _alignmentRepo.Create(new PipeAlignmentCreateDefinition(alignments, origin)); return (result.CreatedCount, result.SkippedSegments, result.Warnings); }
    private static IReadOnlyList<string> GetElevationWarnings(IReadOnlyList<PipeAlignment> alignments, PipeAlignmentModelingRequest request, StraightPipeSpecTable specs)
    {
        var missing = alignments
            .Where(x => specs.Find(x.PipeKind, x.DiameterMm) is null)
            .Select(x => (x.PipeKind, x.DiameterMm))
            .Distinct()
            .ToList();
        if (missing.Count == 0) return Array.Empty<string>();

        var values = string.Join(", ", missing.Select(x => $"{x.PipeKind}/DN{x.DiameterMm:0.##}"));
        if (PipeElevationDatum.NeedsSpec(request.ZDatum))
            return new[] { $"직관 제원이 없어 관종/DN {missing.Count}건은 호칭지름을 외경으로 간주해 높이를 보정했습니다{(request.OutputMode == PipeAlignmentOutputMode.Adaptive ? "(OD·두께 인스턴스 매개변수도 기록하지 못했습니다)" : string.Empty)}: {values}" };
        return request.OutputMode == PipeAlignmentOutputMode.Adaptive
            ? new[] { $"직관 제원이 없어 OD·두께 인스턴스 매개변수를 기록하지 못한 관종/DN {missing.Count}건: {values}" }
            : Array.Empty<string>();
    }
    /// <summary>직관 배치 결과를 스위치 공통 형태로 맞춘다. 직관 배치에는 건너뛴 세그먼트 개념이 없어 Skipped는 0이다.</summary>
    private static (int Count, int Skipped, IReadOnlyList<string> Warnings) ToStraight(AlignmentStraightPlacementResult result, IReadOnlyList<string> bendWarnings)
        => (result.Count, 0, bendWarnings.Concat(result.Warnings).ToList());
    private static string Require(string? value, string name) => !string.IsNullOrWhiteSpace(value) ? value : throw new InvalidOperationException($"{name}을 선택하세요.");
    private static IReadOnlyList<SharedParameterDefinition> GetAlignmentParameterDefinitions() => new List<SharedParameterDefinition>
    {
        new() { Name = "DH_Addin", SpecType = ParameterSpecType.Text, GroupType = ParameterGroupType.Data, BindingType = ParameterBindingType.Instance, Categories = new[] { ParameterCategory.GenericModel } },
        new() { Name = "DH_Category", SpecType = ParameterSpecType.Text, GroupType = ParameterGroupType.Data, BindingType = ParameterBindingType.Instance, Categories = new[] { ParameterCategory.GenericModel } },
        new() { Name = "DH_Part", SpecType = ParameterSpecType.Text, GroupType = ParameterGroupType.Data, BindingType = ParameterBindingType.Instance, Categories = new[] { ParameterCategory.GenericModel } },
        new() { Name = "DH_구경", SpecType = ParameterSpecType.Length, GroupType = ParameterGroupType.Data, BindingType = ParameterBindingType.Instance, Categories = new[] { ParameterCategory.GenericModel } },
        new() { Name = "DH_연장", SpecType = ParameterSpecType.Length, GroupType = ParameterGroupType.Data, BindingType = ParameterBindingType.Instance, Categories = new[] { ParameterCategory.GenericModel } },
        new() { Name = "DH_시점표고", SpecType = ParameterSpecType.Number, GroupType = ParameterGroupType.Data, BindingType = ParameterBindingType.Instance, Categories = new[] { ParameterCategory.GenericModel } },
        new() { Name = "DH_종점표고", SpecType = ParameterSpecType.Number, GroupType = ParameterGroupType.Data, BindingType = ParameterBindingType.Instance, Categories = new[] { ParameterCategory.GenericModel } },
        new() { Name = "DH_원본파일", SpecType = ParameterSpecType.Text, GroupType = ParameterGroupType.Data, BindingType = ParameterBindingType.Instance, Categories = new[] { ParameterCategory.GenericModel } },
        new() { Name = "DH_레코드번호", SpecType = ParameterSpecType.Text, GroupType = ParameterGroupType.Data, BindingType = ParameterBindingType.Instance, Categories = new[] { ParameterCategory.GenericModel } }
    };
}
