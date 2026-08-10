using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Core.Gis;

namespace DHBIMWATER.Application.Gis;

public sealed record AlignmentLoadResult(IReadOnlyList<PipeAlignment> Alignments, int DiameterUnresolvedCount, IReadOnlyList<string> Warnings);

public sealed class AlignmentSourceLoader
{
    private readonly IReadOnlyList<IAlignmentSourceReader> _readers;
    public AlignmentSourceLoader(IEnumerable<IAlignmentSourceReader> readers) => _readers = readers.ToList();

    /// <summary>파일을 읽고 필드 매핑을 적용해 PipeAlignment 목록을 만든다. 트랜잭션·Revit 의존 없음.</summary>
    public AlignmentLoadResult Load(IReadOnlyList<AlignmentSourceFile> files)
    {
        var alignments = new List<PipeAlignment>();
        var warnings = new List<string>();
        var unresolved = 0;
        foreach (var file in files)
        {
            var reader = _readers.FirstOrDefault(r => r.CanRead(file.FilePath))
                ?? throw new InvalidOperationException($"'{file.FilePath}' 파일을 읽을 수 있는 리더가 없습니다.");
            var read = reader is IExcelAlignmentSourceReader excelReader && file.ExcelMapping is not null
                ? excelReader.Read(file.FilePath, file.ExcelMapping)
                : reader.Read(file.FilePath);
            warnings.AddRange(read.Warnings);
            var layers = file.Layers is { Count: > 0 } ? new HashSet<string>(file.Layers, StringComparer.OrdinalIgnoreCase) : null;
            var failed = 0;
            string? example = null;
            foreach (var feature in read.Features)
            {
                if (layers is not null && (!feature.Attributes.TryGetValue("LAYER", out var layer) || !layers.Contains(layer))) continue;
                var kind = ReadValue(feature, file.KindField);
                var rawDiameter = ReadValue(feature, file.DiameterField);
                var parsed = AlignmentAttributeParser.ParseDiameter(rawDiameter);
                if (string.IsNullOrWhiteSpace(kind)) kind = parsed.Kind;
                if (string.IsNullOrWhiteSpace(kind)) kind = file.PipeKind;
                double diameterMm; bool diameterResolved;
                if (parsed.Success) { diameterMm = parsed.DiameterMm; diameterResolved = true; }
                else if (file.ManualDiameterMm is > 0) { diameterMm = file.ManualDiameterMm.Value; diameterResolved = true; }
                else { diameterMm = 0d; diameterResolved = false; }
                if (!diameterResolved)
                {
                    unresolved++; failed++;
                    example ??= rawDiameter;
                }
                alignments.Add(feature with { PipeKind = kind, DiameterMm = diameterMm });
            }
            if (failed > 0)
            {
                var field = file.DiameterField ?? "미지정";
                var value = string.IsNullOrWhiteSpace(example) ? "값 없음" : example;
                warnings.Add($"{Path.GetFileName(file.FilePath)}: 직경 필드 '{field}' 값을 해석하지 못한 레코드 {failed}건 (예: '{value}'). 곡관 판정과 관저·관정 보정이 부정확합니다.");
            }
        }
        return new AlignmentLoadResult(alignments, unresolved, warnings);
    }

    private static string? ReadValue(PipeAlignment feature, string? field)
        => field is not null && feature.Attributes.TryGetValue(field, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;
}
