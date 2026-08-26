using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Application.Gis;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Core.Gis;
using DHBIMWATER.Core.Parameters;
using DHBIMWATER.Shared.Diagnostics;

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

    public PipeAlignmentModelingResult Execute(PipeAlignmentModelingRequest request, IProgress<PipeAlignmentProgress>? progress = null)
    {
        if (request.OutputMode is PipeAlignmentOutputMode.Adaptive or PipeAlignmentOutputMode.PipingSystem && request.IntervalMm <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.IntervalMm));
        // 장거리 배치 성능 병목 비중 확정용 단계별 계측 (2026-08-24).
        // 결과 보고서: %LOCALAPPDATA%\DHBIMWATER\Logs\placement-profile.log
        // TODO: 병목이 확정되면 계측 배선을 걷어낼지 결정할 것.
        PlacementProfiler.Begin($"{request.OutputMode} / 간격 {request.IntervalMm:0}mm / 소스 {request.Files.Count}개");
        try { return ExecuteCore(request, progress); }
        finally { PlacementProfiler.End(); }
    }

    private PipeAlignmentModelingResult ExecuteCore(PipeAlignmentModelingRequest request, IProgress<PipeAlignmentProgress>? progress)
    {
        AlignmentLoadResult loaded;
        progress?.Report(new PipeAlignmentProgress(PipeAlignmentPhase.Loading, 0, 0));
        // DirectShape 형상은 관종 규격을 소비하지 않는다. 원본 관종값은 요소 정보에 그대로 기록하되,
        // 고정 목록 선택을 요구하는 경고만 출력하지 않는다.
        using (PlacementProfiler.Step("01 파일 로드"))
            loaded = _loader.Load(request.Files, warnOnUnknownPipeKinds: request.OutputMode != PipeAlignmentOutputMode.DirectShape);
        // 기준점을 명시하지 않은 호출부에만 자동 산출을 적용한다.
        // (0, 0)을 자동 산출로 대체하면 "원본 좌표 그대로 배치"를 요청할 수 없어 파일마다 오프셋이 달라진다.
        var reference = request.ReferenceX is { } referenceX && request.ReferenceY is { } referenceY
            ? ((double X, double Y)?)(referenceX, referenceY)
            : AlignmentReferencePoint.FromFirstVertex(loaded.Alignments);
        var settingsResolution = request.CurrentBendSettings is null
            ? _bendSettings.Load()
            : new BendSettingsResolution(request.CurrentBendSettings, BendSettingsSource.Project);
        var (settings, settingsSource) = settingsResolution;
        // DirectShape는 선형 자체를 그대로 형상화하므로 원본 Z를 유지한다(ZDatum 보정 없음).
        // 가변 패밀리·파이프는 관 중심선을 배치해야 하므로 종전대로 보정한다.
        var datum = request.OutputMode == PipeAlignmentOutputMode.DirectShape ? ZDatum.Centerline : request.ZDatum;
        var origin = new AlignmentPlacementOrigin(reference?.X ?? 0, reference?.Y ?? 0, datum, settings.StraightPipes);
        var infoContext = new PipeInfoParameterContext(settings.ActiveJointType, settings.ApplicationMode, request.ZDatum);
        var elevationWarnings = GetElevationWarnings(loaded.Alignments, request, settings.StraightPipes);
        // 곡관 자리 계산은 Adaptive 모드에만 적용한다(사용자 결정 2026-08-07, 2026-08-18 명칭 변경).
        // PipingSystem은 Revit이 NewElbowFitting으로 부속을 자동 생성해 이중이 되고,
        // DirectShape는 폴리선 원형을 그대로 형상화하는 경로다.
        BendTrimPlan? bendPlan;
        IReadOnlyList<string> bendWarnings;
        progress?.Report(new PipeAlignmentProgress(PipeAlignmentPhase.PlanningBends, 0, 0));
        using (PlacementProfiler.Step("02 곡관 계획(PlanBends)"))
            (bendPlan, bendWarnings) = request.OutputMode == PipeAlignmentOutputMode.Adaptive
                ? PlanBends(loaded.Alignments, request, settings, settingsSource == BendSettingsSource.BuiltInDefault)
                : (null, Array.Empty<string>());

        // 트랜잭션을 열기 전에 카탈로그가 가리키는 곡관 패밀리가 실제로 이 문서에 로드돼 있는지 확인한다.
        // 그렇지 않으면 직관 배치까지 다 끝낸 뒤 곡관 배치 단계에서야 실패해 전체가 롤백된다.
        if (bendPlan is not null)
        {
            using var bendFamilyScope = PlacementProfiler.Step("03 곡관 패밀리 로드 확인");
            EnsureBendConfiguration(bendPlan, request.BendFamilyTypeName);
            EnsureBendFamilyLoaded(bendPlan, request.BendFamilyTypeName);
        }

        using (_transaction)
        {
            try
            {
                _transaction.Begin(request.OutputMode == PipeAlignmentOutputMode.DirectShape ? "Import Pipe Alignment" : "선형 패밀리 배치", suppressWarnings: true);
                using (PlacementProfiler.Step("04 공유 매개변수 확보"))
                {
                    if (request.OutputMode == PipeAlignmentOutputMode.DirectShape) _sharedParameterRepo.EnsureParameters(GetAlignmentParameterDefinitions());
                    else if (request.OutputMode == PipeAlignmentOutputMode.Adaptive) _sharedParameterRepo.EnsureParameters(PipeAlignmentParameters.Definitions);
                }
                if (request.ApplySharedCoordinates)
                {
                    using var sharedCoordinateScope = PlacementProfiler.Step("05 공유좌표 설정");
                    _projectLocationRepo.SetInternalOriginSharedPosition(origin.X, origin.Y, 0);
                }
                AlignmentStraightPlacementResult? straightResult = null;
                int count;
                int skipped;
                IReadOnlyList<string> repoWarnings;
                using (PlacementProfiler.Step("06 직관/파이프 배치 전체"))
                    (count, skipped, repoWarnings) = request.OutputMode switch
                {
                    PipeAlignmentOutputMode.DirectShape => ToDirectShape(loaded.Alignments, origin, progress),
                    PipeAlignmentOutputMode.Adaptive => ToStraight(straightResult = _straightRepo.PlaceAlong(loaded.Alignments, Require(request.StraightFamilyTypeName, "직관 패밀리"), request.IntervalMm / 1000d, origin, settings.StraightPipes, bendPlan?.Plans.Select(x => x.Trims).ToList(), request.StraightDiameterParameterName, request.StraightOuterDiameterParameterName, request.StraightThicknessParameterName, infoContext, progress), bendWarnings),
                    PipeAlignmentOutputMode.PipingSystem => (_pipeRepo.PlaceAlong(loaded.Alignments, Require(request.PipingSystemTypeName, "파이프 시스템 유형"), Require(request.PipeTypeName, "PipeType"), request.LevelName, request.IntervalMm / 1000d, origin), 0, (IReadOnlyList<string>)Array.Empty<string>()),
                    _ => throw new ArgumentOutOfRangeException(nameof(request.OutputMode))
                };
                AdaptiveBendPlacementResult bendResult;
                using (PlacementProfiler.Step("07 곡관 배치 전체"))
                    bendResult = request.OutputMode == PipeAlignmentOutputMode.Adaptive && bendPlan is not null
                        ? PlaceBendFittings(bendPlan, loaded.Alignments, origin, request, infoContext, progress)
                        : new AdaptiveBendPlacementResult(0, Array.Empty<string>());
                progress?.Report(new PipeAlignmentProgress(PipeAlignmentPhase.Committing, 0, 0));
                using (PlacementProfiler.Step("09 트랜잭션 Commit")) _transaction.Commit();
                var suppressedWarningSummary = SummarizeSuppressedWarnings(_transaction.SuppressedWarnings);
                return new PipeAlignmentModelingResult(request.OutputMode, count, skipped,
                    loaded.Warnings.Concat(elevationWarnings).Concat(repoWarnings).Concat(bendResult.Warnings)
                        .Concat(FindOuterDiameterMismatches(straightResult, bendResult)).Concat(suppressedWarningSummary).ToList(),
                    bendResult.Count);
            }
            catch { _transaction.Rollback(); throw; }
        }
    }

    private static void EnsureBendConfiguration(BendTrimPlan bendPlan, string? familyTypeName)
    {
        if (bendPlan.Placements.Count == 0) return;
        ParseFamilyType(familyTypeName);
    }

    private void EnsureBendFamilyLoaded(BendTrimPlan bendPlan, string? familyTypeName)
    {
        if (bendPlan.Placements.Count == 0) return;
        var pair = ParseFamilyType(familyTypeName);

        var missing = _adaptiveBendRepo.FindMissingSymbols(new[] { pair });
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
        if (settings.Fittings.NeedsRestore) warnings.Add("저장된 곡관 치수가 현행 규격표와 구조가 다릅니다(중복 행 또는 플랜지곡관 누락). [관·곡관 규격표] 창에서 [기본값 복원] 후 저장하세요.");

        var snapTolerance = request.SnapToleranceMm / 1000d;
        var graph = PipeNetworkBuilder.Build(alignments, snapTolerance);
        var nodes = PipeNetworkClassifier.Classify(graph);
        var resolutions = BendResolver.ResolveAll(nodes, settings);
        var plan = BendTrimPlanner.Plan(alignments, graph, resolutions, snapTolerance);
        warnings.AddRange(plan.Warnings);

        var skipped = resolutions.Count(x => x.Kind != BendResolutionKind.None && !x.HasFittingSize);
        if (skipped > 0)
            warnings.Add($"곡관 치수가 없어 배치하지 못한 절점 {skipped}곳은 직관을 절점까지 붙였습니다.");

        var exceeded = resolutions.Where(x => !x.IsAcceptable).Select(x => x.NodeId).ToList();
        if (exceeded.Count > 0)
            warnings.Add($"허용굴곡 초과 절점 {exceeded.Count}곳: {string.Join(", ", exceeded)}. 배치 가능한 곡관은 3D 뷰에서 적색 표시합니다.");

        return (plan, warnings);
    }

    /// <summary>선택한 단일 패밀리·타입으로 모든 5점 가변 곡관을 배치한다.</summary>
    private AdaptiveBendPlacementResult PlaceBendFittings(BendTrimPlan plan, IReadOnlyList<PipeAlignment> alignments, AlignmentPlacementOrigin origin, PipeAlignmentModelingRequest request, PipeInfoParameterContext infoContext, IProgress<PipeAlignmentProgress>? progress)
    {
        if (plan.Placements.Count == 0) return new AdaptiveBendPlacementResult(0, Array.Empty<string>());
        var (familyName, typeName) = ParseFamilyType(request.BendFamilyTypeName);

        // OD는 곡관 형상 구동값이라 못 구하면 패밀리 기본 굵기로 남는다. (관종, DN)이 없으면 DN만으로 폴백한다(사용자 결정 2026-08-19).
        var outerDiameterFallbacks = new HashSet<(string PipeKind, double DiameterMm, string SourceKind, double OuterDiameterMm)>();
        var bendPlans = plan.Placements.Select(x =>
        {
            var alignment = alignments[x.AlignmentIndex];
            var spec = origin.StraightPipeSpecs.Find(x.PipeKind, x.DiameterMm);
            var outerDiameterMm = spec?.OuterDiameterMm;
            if (outerDiameterMm is null && origin.StraightPipeSpecs.FindOuterDiameterByDiameter(x.DiameterMm) is { } fallback)
            {
                outerDiameterMm = fallback.OuterDiameterMm;
                outerDiameterFallbacks.Add((x.PipeKind, x.DiameterMm, fallback.PipeKind, fallback.OuterDiameterMm));
            }
            return new AdaptiveBendPlacementPlan(
                x.NodeId, familyName, typeName, x.Points, x.DiameterMm, x.WallThicknessMm,
                origin.GetZOffsetM(x.PipeKind, x.DiameterMm), request.BendDiameterParameterName, request.BendWallThicknessParameterName, request.BendOuterDiameterParameterName, x.IsAcceptable,
                x.RotXYDeg, x.RotXZDeg, x.PipeKind, alignment.SourceFile, alignment.RecordNumber, outerDiameterMm,
                x.DeflectionDeg, x.AngleDeg, x.EffectiveAllowableDeg, x.ResidualDeg, x.CenterlineRadiusMm, x.LayingLengthMm,
                x.Form, x.WeightKg);
        }).ToList();
        var result = _adaptiveBendRepo.Place(bendPlans, origin, infoContext, progress);
        if (outerDiameterFallbacks.Count == 0) return result;
        // 다른 관종 값을 대신 쓴 것이므로 조용히 넘어가지 않고 어떤 관종에서 가져왔는지 알린다.
        var fallbackNotice = $"직관 제원표에 없는 관종/DN {outerDiameterFallbacks.Count}건은 같은 DN의 다른 관종 외경으로 곡관을 배치했습니다(외경은 관종과 무관합니다): "
            + string.Join(", ", outerDiameterFallbacks
                .OrderBy(x => x.PipeKind).ThenBy(x => x.DiameterMm)
                .Select(x => $"{x.PipeKind}/DN{x.DiameterMm:0.##} ← {x.SourceKind} {x.OuterDiameterMm:0.##}mm"));
        return result with { Warnings = result.Warnings.Prepend(fallbackNotice).ToList() };
    }

    private static (string FamilyName, string TypeName) ParseFamilyType(string? familyTypeName)
    {
        if (string.IsNullOrWhiteSpace(familyTypeName))
            throw new InvalidOperationException("배치할 곡관이 있습니다. 곡관 패밀리·유형을 선택하세요.");
        var separator = familyTypeName.LastIndexOf(" : ", StringComparison.Ordinal);
        if (separator <= 0 || separator >= familyTypeName.Length - 3)
            throw new InvalidOperationException($"곡관 패밀리·유형 표기가 '패밀리명 : 유형명' 형식이 아닙니다: {familyTypeName}");
        return (familyTypeName[..separator], familyTypeName[(separator + 3)..]);
    }

    private (int Count, int Skipped, IReadOnlyList<string> Warnings) ToDirectShape(IReadOnlyList<PipeAlignment> alignments, AlignmentPlacementOrigin origin, IProgress<PipeAlignmentProgress>? progress)
    { var result = _alignmentRepo.Create(new PipeAlignmentCreateDefinition(alignments, origin), progress); return (result.CreatedCount, result.SkippedSegments, result.Warnings); }
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
    private static IReadOnlyList<string> FindOuterDiameterMismatches(AlignmentStraightPlacementResult? straight, AdaptiveBendPlacementResult bends)
    {
        if (straight?.OuterDiametersMm is null || bends.OuterDiametersMm is null) return Array.Empty<string>();
        var mismatches = bends.OuterDiametersMm
            .Where(x => straight.OuterDiametersMm.TryGetValue(x.Key, out var straightOd) && Math.Abs(straightOd - x.Value) > 1e-6)
            .Select(x => $"{x.Key.PipeKind}/DN{x.Key.DiameterMm:0.##}: 직관 {straight.OuterDiametersMm[x.Key]:0.##}mm, 곡관 {x.Value:0.##}mm")
            .ToList();
        return mismatches.Count == 0 ? Array.Empty<string>() : new[] { $"직관과 곡관에 기록한 OD가 다른 관종/DN {mismatches.Count}건: {string.Join(", ", mismatches)}" };
    }
    /// <summary>같은 경고 문구가 요소마다 반복되므로(예: "짧은 곡선 요소") 문구별 건수로 묶어 한 줄씩만 보여준다.</summary>
    private static IReadOnlyList<string> SummarizeSuppressedWarnings(IReadOnlyList<string> messages)
    {
        if (messages.Count == 0) return Array.Empty<string>();
        var grouped = messages.GroupBy(x => x).OrderByDescending(x => x.Count());
        return new[] { $"Revit 경고 {messages.Count}건을 자동으로 무시하고 배치를 계속했습니다: "
            + string.Join(", ", grouped.Select(x => $"{x.Key} {x.Count()}건")) };
    }
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
