using System.Globalization;
using System.Text.RegularExpressions;
using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Core.Gis;

namespace DHBIMWATER.Application.UseCases.Gis;

public sealed class PlaceAlignmentFamilyUseCase
{
    private static readonly Regex CombinedDiameter = new("^(?<kind>.*?)_D(?<dia>\\d+)$", RegexOptions.Compiled);
    private readonly ITransactionContext _transaction;
    private readonly IReadOnlyList<IAlignmentSourceReader> _readers;
    private readonly IAlignmentBeamPlacementRepo _beamRepo;
    private readonly IAlignmentPipePlacementRepo _pipeRepo;
    private readonly IProjectLocationCommandRepo _projectLocationRepo;

    public PlaceAlignmentFamilyUseCase(ITransactionContext transaction, IEnumerable<IAlignmentSourceReader> readers, IAlignmentBeamPlacementRepo beamRepo, IAlignmentPipePlacementRepo pipeRepo, IProjectLocationCommandRepo projectLocationRepo)
    { _transaction = transaction; _readers = readers.ToList(); _beamRepo = beamRepo; _pipeRepo = pipeRepo; _projectLocationRepo = projectLocationRepo; }

    public AlignmentFamilyPlacementResult Execute(AlignmentFamilyPlacementRequest request)
    {
        if (request.IntervalMm <= 0) throw new ArgumentOutOfRangeException(nameof(request.IntervalMm));
        var warnings = new List<string>(); var alignments = new List<PipeAlignment>();
        foreach (var file in request.Files)
        {
            var reader = _readers.FirstOrDefault(r => r.CanRead(file.FilePath)) ?? throw new InvalidOperationException($"'{file.FilePath}' 파일을 읽을 수 있는 리더가 없습니다.");
            var read = reader.Read(file.FilePath); warnings.AddRange(read.Warnings);
            alignments.AddRange(read.Features.Select(x => ApplyAttributes(x, file.PipeKind, request.ParseCombinedDiameter, warnings)));
        }
        var reference = request.ReferenceX == 0 && request.ReferenceY == 0 && request.ReferenceZ == 0
            ? AlignmentReferencePoint.FromFirstVertex(alignments)
            : (request.ReferenceX, request.ReferenceY, request.ReferenceZ);
        var origin = new AlignmentPlacementOrigin(reference?.X ?? 0, reference?.Y ?? 0, reference?.Z ?? 0, request.ZDatum);
        using (_transaction)
        {
            try
            {
                _transaction.Begin("선형 패밀리 배치");
                if (request.ApplySharedCoordinates)
                    _projectLocationRepo.SetInternalOriginSharedPosition(origin.X, origin.Y, origin.Z, 0);
                var count = request.Target switch
                {
                    AlignmentPlacementTarget.Beam => _beamRepo.PlaceAlong(alignments, Require(request.BeamTypeName, "빔 유형"), request.LevelName, request.IntervalMm / 1000d, request.AlignTangent, origin),
                    AlignmentPlacementTarget.PipingSystem => _pipeRepo.PlaceAlong(alignments, Require(request.PipingSystemTypeName, "파이프 시스템 유형"), Require(request.PipeTypeName, "PipeType"), request.LevelName, request.IntervalMm / 1000d, origin),
                    _ => throw new ArgumentOutOfRangeException(nameof(request.Target))
                };
                _transaction.Commit(); return new AlignmentFamilyPlacementResult(count, warnings);
            }
            catch { _transaction.Rollback(); throw; }
        }
    }
    private static string Require(string? value, string name) => !string.IsNullOrWhiteSpace(value) ? value : throw new InvalidOperationException($"{name}을 선택하세요.");
    private static PipeAlignment ApplyAttributes(PipeAlignment alignment, string fileKind, bool parse, List<string> warnings)
    {
        if (!parse || !alignment.Attributes.TryGetValue("Diameter", out var raw)) return alignment with { PipeKind = fileKind };
        var match = CombinedDiameter.Match(raw);
        if (!match.Success) { warnings.Add($"{alignment.SourceFile} 레코드 {alignment.RecordNumber}: Diameter '{raw}' 형식을 해석하지 못했습니다."); return alignment with { PipeKind = raw }; }
        return alignment with { PipeKind = string.IsNullOrWhiteSpace(match.Groups["kind"].Value) ? fileKind : match.Groups["kind"].Value, DiameterMm = double.Parse(match.Groups["dia"].Value, CultureInfo.InvariantCulture) };
    }
}
