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
    private readonly IAlignmentBeamPlacementRepo _beamRepo;
    private readonly IAlignmentPipePlacementRepo _pipeRepo;
    private readonly IProjectLocationCommandRepo _projectLocationRepo;
    private readonly ISharedParameterRepository _sharedParameterRepo;
    private readonly IBendSettingsRepo _bendSettingsRepo;
    private readonly IAdaptiveBendPlacementRepo _adaptiveBendRepo;

    public ModelPipeAlignmentUseCase(ITransactionContext transaction, AlignmentSourceLoader loader, IPipeAlignmentCommandRepo alignmentRepo,
        IAlignmentBeamPlacementRepo beamRepo, IAlignmentPipePlacementRepo pipeRepo, IProjectLocationCommandRepo projectLocationRepo,
        ISharedParameterRepository sharedParameterRepo, IBendSettingsRepo bendSettingsRepo, IAdaptiveBendPlacementRepo adaptiveBendRepo)
    { _transaction = transaction; _loader = loader; _alignmentRepo = alignmentRepo; _beamRepo = beamRepo; _pipeRepo = pipeRepo; _projectLocationRepo = projectLocationRepo; _sharedParameterRepo = sharedParameterRepo; _bendSettingsRepo = bendSettingsRepo; _adaptiveBendRepo = adaptiveBendRepo; }

    public PipeAlignmentModelingResult Execute(PipeAlignmentModelingRequest request)
    {
        if (request.OutputMode is PipeAlignmentOutputMode.Beam or PipeAlignmentOutputMode.PipingSystem && request.IntervalMm <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.IntervalMm));
        var loaded = _loader.Load(request.Files);
        var reference = request.ReferenceX == 0 && request.ReferenceY == 0
            ? AlignmentReferencePoint.FromFirstVertex(loaded.Alignments)
            : ((double X, double Y)?)(request.ReferenceX, request.ReferenceY);
        var origin = new AlignmentPlacementOrigin(reference?.X ?? 0, reference?.Y ?? 0, request.ZDatum);
        // 곡관 자리 계산은 Beam 모드에만 적용한다(사용자 결정 2026-08-07).
        // PipingSystem은 Revit이 NewElbowFitting으로 부속을 자동 생성해 이중이 되고,
        // DirectShape는 폴리선 원형을 그대로 형상화하는 경로다.
        var (bendPlan, bendWarnings) = request.OutputMode == PipeAlignmentOutputMode.Beam
            ? PlanBends(loaded.Alignments, request)
            : (null, Array.Empty<string>());

        // 트랜잭션을 열기 전에 카탈로그가 가리키는 곡관 패밀리가 실제로 이 문서에 로드돼 있는지 확인한다.
        // 그렇지 않으면 직관 배치까지 다 끝낸 뒤 곡관 배치 단계에서야 실패해 전체가 롤백된다.
        if (bendPlan is not null) EnsureBendFamiliesLoaded(bendPlan);

        using (_transaction)
        {
            try
            {
                _transaction.Begin(request.OutputMode == PipeAlignmentOutputMode.DirectShape ? "Import Pipe Alignment" : "선형 패밀리 배치");
                if (request.OutputMode == PipeAlignmentOutputMode.DirectShape) _sharedParameterRepo.EnsureParameters(GetAlignmentParameterDefinitions());
                if (request.ApplySharedCoordinates) _projectLocationRepo.SetInternalOriginSharedPosition(origin.X, origin.Y, 0);
                var (count, skipped, repoWarnings) = request.OutputMode switch
                {
                    PipeAlignmentOutputMode.DirectShape => ToDirectShape(loaded.Alignments, origin),
                    PipeAlignmentOutputMode.Beam => (_beamRepo.PlaceAlong(loaded.Alignments, Require(request.BeamTypeName, "빔 유형"), request.LevelName, request.IntervalMm / 1000d, request.AlignTangent, origin, bendPlan?.Plans.Select(x => x.Trims).ToList(), request.BeamDiameterParameterName, request.BeamKindParameterName), 0, (IReadOnlyList<string>)bendWarnings),
                    PipeAlignmentOutputMode.PipingSystem => (_pipeRepo.PlaceAlong(loaded.Alignments, Require(request.PipingSystemTypeName, "파이프 시스템 유형"), Require(request.PipeTypeName, "PipeType"), request.LevelName, request.IntervalMm / 1000d, origin), 0, (IReadOnlyList<string>)Array.Empty<string>()),
                    _ => throw new ArgumentOutOfRangeException(nameof(request.OutputMode))
                };
                var placementWarnings = request.OutputMode == PipeAlignmentOutputMode.Beam && bendPlan is not null
                    ? PlaceBendFittings(bendPlan, origin)
                    : Array.Empty<string>();
                _transaction.Commit();
                return new PipeAlignmentModelingResult(request.OutputMode, count, skipped, loaded.Warnings.Concat(repoWarnings).Concat(placementWarnings).ToList(), bendPlan?.Placements.Count ?? 0);
            }
            catch { _transaction.Rollback(); throw; }
        }
    }

    private void EnsureBendFamiliesLoaded(BendTrimPlan bendPlan)
    {
        var pairs = bendPlan.Placements
            .Where(x => x.FamilyName is not null && x.TypeName is not null)
            .Select(x => (x.FamilyName!, x.TypeName!))
            .ToList();
        if (pairs.Count == 0) return;

        var missing = _adaptiveBendRepo.FindMissingSymbols(pairs);
        if (missing.Count > 0)
            throw new InvalidOperationException($"다음 곡관 패밀리·타입을 문서에서 찾을 수 없습니다(로드 필요): {string.Join(", ", missing.Select(x => $"{x.FamilyName}:{x.TypeName}"))}");
    }

    /// <summary>
    /// 절점 곡관 판정을 폴리선 정점 차감량으로 환산한다. 곡관 실물(RFA) 배치는 아직 하지 않고 자리만 비운다.
    /// 설정이 없거나 곡관이 하나도 없으면 null을 돌려 기존(차감 없음) 동작을 그대로 쓴다.
    /// </summary>
    private (BendTrimPlan? Plan, IReadOnlyList<string> Warnings) PlanBends(IReadOnlyList<PipeAlignment> alignments, PipeAlignmentModelingRequest request)
    {
        if (request.SnapToleranceMm <= 0) throw new ArgumentOutOfRangeException(nameof(request.SnapToleranceMm));

        var warnings = new List<string>();
        var settings = _bendSettingsRepo.Load();
        if (settings is null)
        {
            warnings.Add("허용굴곡·곡관 치수 설정이 저장되지 않아 곡관 자리를 비우지 않고 직관을 절점까지 붙였습니다.");
            return (null, warnings);
        }

        var snapTolerance = request.SnapToleranceMm / 1000d;
        var graph = PipeNetworkBuilder.Build(alignments, snapTolerance);
        var nodes = PipeNetworkClassifier.Classify(graph);
        var resolutions = BendResolver.ResolveAll(nodes, settings, request.Form);
        var plan = BendTrimPlanner.Plan(alignments, graph, resolutions, snapTolerance);
        warnings.AddRange(plan.Warnings);

        // 곡관을 넣지 못한 절점은 직관이 절점까지 그대로 붙는다. 배치 결과만 보면 알 수 없으므로 집계해 남긴다.
        var skipped = resolutions.Count(x => x.Kind == BendResolutionKind.Unresolved)
            + resolutions.Count(x => x.Kind == BendResolutionKind.Standard && !x.HasFittingSize);
        if (skipped > 0)
            warnings.Add($"곡관을 넣지 못한 절점 {skipped}곳은 직관을 절점까지 붙였습니다(미해결 편각 또는 곡관 치수 미입력).");

        // t/R은 있지만 패밀리·타입이 카탈로그에 없는 절점 — 직관 차감은 되지만 실물은 안 들어간다.
        var missingFamily = plan.Placements.Count(x => x.FamilyName is null || x.TypeName is null);
        if (missingFamily > 0)
            warnings.Add($"곡관 패밀리·타입이 설정되지 않아 실물 배치를 생략한 절점 {missingFamily}곳은 자리만 비웠습니다.");

        return (plan, warnings);
    }

    /// <summary>카탈로그에 패밀리·타입이 등록된 절점만 골라 5점 가변 곡관을 배치한다.</summary>
    private IReadOnlyList<string> PlaceBendFittings(BendTrimPlan plan, AlignmentPlacementOrigin origin)
    {
        var placeable = plan.Placements.Where(x => x.FamilyName is not null && x.TypeName is not null).ToList();
        if (placeable.Count == 0) return Array.Empty<string>();

        var bendPlans = placeable.Select(x => new AdaptiveBendPlacementPlan(
            x.NodeId, x.FamilyName!, x.TypeName!, x.Points, x.DiameterMm, x.WallThicknessMm,
            x.RotXYDeg, x.RotXZDeg)).ToList();
        return _adaptiveBendRepo.Place(bendPlans, origin).Warnings;
    }

    private (int Count, int Skipped, IReadOnlyList<string> Warnings) ToDirectShape(IReadOnlyList<PipeAlignment> alignments, AlignmentPlacementOrigin origin)
    { var result = _alignmentRepo.Create(new PipeAlignmentCreateDefinition(alignments, origin.X, origin.Y, origin.ZDatum)); return (result.CreatedCount, result.SkippedSegments, result.Warnings); }
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
