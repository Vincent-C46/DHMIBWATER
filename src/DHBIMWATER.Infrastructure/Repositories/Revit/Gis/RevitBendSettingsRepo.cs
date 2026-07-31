using System.Text.Json;
using System.Text.Json.Serialization;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using DHBIMWATER.Application.Interfaces.Gis;
using DHBIMWATER.Core.Gis;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Gis;

/// <summary>
/// 허용굴곡 표와 곡관 치수 카탈로그를 DataStorage 엘리먼트 1개에 ExtensibleStorage로 저장한다.
/// Transaction은 열지 않는다 — 반드시 UseCase 트랜잭션 안에서 호출된다.
/// </summary>
public sealed class RevitBendSettingsRepo : IBendSettingsRepo
{
    private static readonly Guid SchemaGuid = new("6B1E9C74-2A38-4F55-9E0D-7C4A81F26B33");
    private const string SchemaName = "DHBIMWATER_BendSettings";
    private const string StorageName = "DHBIMWATER_BendSettings";
    private const string ToleranceField = "Tolerances";
    private const string FittingField = "Fittings";

    /// <summary>BendForm을 "AType"처럼 이름으로 남겨 스키마 변경에 견디게 한다.</summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly Func<Document?> _doc;
    public RevitBendSettingsRepo(Func<Document?> doc) => _doc = doc;

    public BendSettings? Load()
    {
        var doc = _doc() ?? throw new InvalidOperationException("활성 Revit 문서를 찾을 수 없습니다.");
        var schema = Schema.Lookup(SchemaGuid);
        if (schema is null) return null;

        var storage = FindStorage(doc);
        if (storage is null) return null;

        var entity = storage.GetEntity(schema);
        if (!entity.IsValid()) return null;

        var tolerances = Deserialize<BendToleranceEntry>(entity.Get<string>(schema.GetField(ToleranceField)));
        // Fittings 필드는 나중에 추가됐다. 구버전 데이터에는 없을 수 있으므로 빈 카탈로그로 복원한다.
        var fittings = schema.GetField(FittingField) is null
            ? new List<BendFittingEntry>()
            : Deserialize<BendFittingEntry>(entity.Get<string>(schema.GetField(FittingField)));

        return new BendSettings(new BendToleranceTable(tolerances), new BendFittingCatalog(fittings));
    }

    public void Save(BendSettings settings)
    {
        var doc = _doc() ?? throw new InvalidOperationException("활성 Revit 문서를 찾을 수 없습니다.");
        var schema = GetOrCreateSchema();
        var storage = FindStorage(doc) ?? CreateStorage(doc);

        var entity = new Entity(schema);
        entity.Set(schema.GetField(ToleranceField), JsonSerializer.Serialize(settings.Tolerance.Entries, JsonOptions));
        entity.Set(schema.GetField(FittingField), JsonSerializer.Serialize(settings.Fittings.Entries, JsonOptions));
        storage.SetEntity(entity);
    }

    private static List<T> Deserialize<T>(string? json)
        => string.IsNullOrWhiteSpace(json) ? new List<T>() : JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? new List<T>();

    private static DataStorage? FindStorage(Document doc)
        => new FilteredElementCollector(doc)
            .OfClass(typeof(DataStorage))
            .Cast<DataStorage>()
            .FirstOrDefault(x => string.Equals(x.Name, StorageName, StringComparison.Ordinal));

    /// <summary>DataStorage 생성도 호출부 트랜잭션 안에서 일어난다.</summary>
    private static DataStorage CreateStorage(Document doc)
    {
        var storage = DataStorage.Create(doc);
        storage.Name = StorageName;
        return storage;
    }

    private static Schema GetOrCreateSchema()
    {
        var schema = Schema.Lookup(SchemaGuid);
        if (schema != null) return schema;

        var builder = new SchemaBuilder(SchemaGuid);
        builder.SetSchemaName(SchemaName);
        builder.SetReadAccessLevel(AccessLevel.Public);
        builder.SetWriteAccessLevel(AccessLevel.Public);
        builder.AddSimpleField(ToleranceField, typeof(string));
        builder.AddSimpleField(FittingField, typeof(string));
        return builder.Finish();
    }
}
