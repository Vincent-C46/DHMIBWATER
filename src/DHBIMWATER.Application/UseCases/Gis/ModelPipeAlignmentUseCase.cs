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

    public ModelPipeAlignmentUseCase(ITransactionContext transaction, AlignmentSourceLoader loader, IPipeAlignmentCommandRepo alignmentRepo,
        IAlignmentBeamPlacementRepo beamRepo, IAlignmentPipePlacementRepo pipeRepo, IProjectLocationCommandRepo projectLocationRepo,
        ISharedParameterRepository sharedParameterRepo)
    { _transaction = transaction; _loader = loader; _alignmentRepo = alignmentRepo; _beamRepo = beamRepo; _pipeRepo = pipeRepo; _projectLocationRepo = projectLocationRepo; _sharedParameterRepo = sharedParameterRepo; }

    public PipeAlignmentModelingResult Execute(PipeAlignmentModelingRequest request)
    {
        if (request.OutputMode is PipeAlignmentOutputMode.Beam or PipeAlignmentOutputMode.PipingSystem && request.IntervalMm <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.IntervalMm));
        var loaded = _loader.Load(request.Files);
        var reference = request.ReferenceX == 0 && request.ReferenceY == 0
            ? AlignmentReferencePoint.FromFirstVertex(loaded.Alignments)
            : ((double X, double Y)?)(request.ReferenceX, request.ReferenceY);
        var origin = new AlignmentPlacementOrigin(reference?.X ?? 0, reference?.Y ?? 0, request.ZDatum);
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
                    PipeAlignmentOutputMode.Beam => (_beamRepo.PlaceAlong(loaded.Alignments, Require(request.BeamTypeName, "빔 유형"), request.LevelName, request.IntervalMm / 1000d, request.AlignTangent, origin), 0, (IReadOnlyList<string>)Array.Empty<string>()),
                    PipeAlignmentOutputMode.PipingSystem => (_pipeRepo.PlaceAlong(loaded.Alignments, Require(request.PipingSystemTypeName, "파이프 시스템 유형"), Require(request.PipeTypeName, "PipeType"), request.LevelName, request.IntervalMm / 1000d, origin), 0, (IReadOnlyList<string>)Array.Empty<string>()),
                    _ => throw new ArgumentOutOfRangeException(nameof(request.OutputMode))
                };
                _transaction.Commit();
                return new PipeAlignmentModelingResult(request.OutputMode, count, skipped, loaded.Warnings.Concat(repoWarnings).ToList());
            }
            catch { _transaction.Rollback(); throw; }
        }
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
