using System.Text.RegularExpressions;
using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Core.Gis;
using DHBIMWATER.Core.Parameters;

namespace DHBIMWATER.Application.UseCases.Gis;

public sealed class ImportPipeAlignmentUseCase
{
    private static readonly Regex CombinedDiameter = new("^(?<kind>.*?)_D(?<dia>\\d+)$", RegexOptions.Compiled);
    private readonly ITransactionContext _transaction;
    private readonly IReadOnlyList<IAlignmentSourceReader> _readers;
    private readonly IPipeAlignmentCommandRepo _alignmentRepo;
    private readonly IProjectLocationCommandRepo _projectLocationRepo;
    private readonly ISharedParameterRepository _sharedParameterRepo;

    public ImportPipeAlignmentUseCase(ITransactionContext transaction, IEnumerable<IAlignmentSourceReader> readers,
        IPipeAlignmentCommandRepo alignmentRepo, IProjectLocationCommandRepo projectLocationRepo,
        ISharedParameterRepository sharedParameterRepo)
    {
        _transaction = transaction; _readers = readers.ToList(); _alignmentRepo = alignmentRepo;
        _projectLocationRepo = projectLocationRepo; _sharedParameterRepo = sharedParameterRepo;
    }

    public PipeAlignmentCreateResult Execute(PipeAlignmentImportRequest request)
    {
        var warnings = new List<string>();
        var alignments = new List<PipeAlignment>();
        foreach (var file in request.Files)
        {
            var reader = _readers.FirstOrDefault(r => r.CanRead(file.ShpPath))
                ?? throw new InvalidOperationException($"'{file.ShpPath}' 파일을 읽을 수 있는 리더가 없습니다.");
            var read = reader.Read(file.ShpPath);
            warnings.AddRange(read.Warnings);
            alignments.AddRange(read.Features.Select(feature => ApplyAttributes(feature, file.PipeKind, request.ParseCombinedDiameter, warnings)));
        }

        using (_transaction)
        {
            try
            {
                _transaction.Begin("Import Pipe Alignment");
                _sharedParameterRepo.EnsureParameters(GetAlignmentParameterDefinitions());
                if (request.ApplySharedCoordinates)
                    _projectLocationRepo.SetInternalOriginSharedPosition(request.ReferenceX, request.ReferenceY, request.ReferenceZ, 0);
                var result = _alignmentRepo.Create(new PipeAlignmentCreateDefinition(alignments, request.ReferenceX, request.ReferenceY, request.ReferenceZ, request.ZDatum));
                _transaction.Commit();
                return result with { Warnings = warnings.Concat(result.Warnings).ToList() };
            }
            catch { _transaction.Rollback(); throw; }
        }
    }

    private static PipeAlignment ApplyAttributes(PipeAlignment alignment, string filePipeKind, bool parseCombined, List<string> warnings)
    {
        var kind = filePipeKind;
        var diameter = 0d;
        if (parseCombined && alignment.Attributes.TryGetValue("Diameter", out var raw))
        {
            var match = CombinedDiameter.Match(raw);
            if (match.Success)
            {
                kind = string.IsNullOrWhiteSpace(match.Groups["kind"].Value) ? filePipeKind : match.Groups["kind"].Value;
                diameter = double.Parse(match.Groups["dia"].Value, System.Globalization.CultureInfo.InvariantCulture);
            }
            else { kind = raw; warnings.Add($"{alignment.SourceFile} 레코드 {alignment.RecordNumber}: Diameter '{raw}' 형식을 해석하지 못했습니다."); }
        }
        return alignment with { PipeKind = kind, DiameterMm = diameter };
    }

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
