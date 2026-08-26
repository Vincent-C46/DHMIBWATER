using DHBIMWATER.Application.DTOs.Gis;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Core.Gis;
using System.IO;

namespace DHBIMWATER.Infrastructure.Repositories.Gis;

public sealed class ShapefileReader : IShapefileReader, IAlignmentSourceReader
{
    public bool CanRead(string filePath) => string.Equals(Path.GetExtension(filePath), ".shp", StringComparison.OrdinalIgnoreCase);

    public ShapefileReadResult Read(string shpPath)
    {
        if (!File.Exists(shpPath)) throw new FileNotFoundException("SHP 파일을 찾을 수 없습니다.", shpPath);
        var dbfPath = Path.ChangeExtension(shpPath, ".dbf");
        if (!File.Exists(dbfPath)) throw new FileNotFoundException("SHP 형제 .dbf 파일이 필요합니다.", dbfPath);
        var warnings = new List<string>();
        var cpgPath = Path.ChangeExtension(shpPath, ".cpg");
        var prjPath = Path.ChangeExtension(shpPath, ".prj");
        if (!File.Exists(cpgPath)) warnings.Add(".cpg 파일이 없어 CP949로 해석했습니다.");
        if (!File.Exists(prjPath)) warnings.Add(".prj 파일이 없어 좌표계 정보를 확인할 수 없습니다.");
        var shp = ShpGeometryReader.Read(shpPath);
        var dbf = DbfTableReader.Read(dbfPath, cpgPath);
        var prj = PrjFileReader.Read(prjPath);
        if (shp.Records.Count != dbf.Rows.Count) warnings.Add($"SHP 레코드 {shp.Records.Count}건과 DBF 레코드 {dbf.Rows.Count}건이 달라 최소 건수만 매칭했습니다.");
        var features = new List<PipeAlignment>();
        foreach (var pair in shp.Records.Take(Math.Min(shp.Records.Count, dbf.Rows.Count)).Select((record, index) => (record, index)))
            for (var partIndex = 0; partIndex < pair.record.Parts.Count; partIndex++)
                features.Add(new PipeAlignment(pair.record.Parts[partIndex], string.Empty, 0, Path.GetFileName(shpPath), $"{pair.record.RecordNumber}+{partIndex + 1}", dbf.Rows[pair.index]));
        var vertexCount = features.Sum(x => x.Vertices.Count);
        return new ShapefileReadResult(features, dbf.Fields, shp.Extent, prj.Wkt, prj.RecommendedEpsg, dbf.EncodingName,
            shp.Records.Count, vertexCount, features.Sum(x => Math.Max(0, x.Vertices.Count - 1)), warnings, dbf.Rows.FirstOrDefault());
    }
}
