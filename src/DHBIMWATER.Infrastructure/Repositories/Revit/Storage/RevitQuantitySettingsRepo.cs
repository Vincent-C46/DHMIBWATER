using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using DHBIMWATER.Application.Interfaces.Storage;
using DHBIMWATER.Core.Settings;
using DHBIMWATER.Infrastructure.Storage.Schemas;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Storage
{
    /// <summary>
    /// 수량산출 설정(ProjectSettings)을 DataStorage에 단일 JSON으로 저장/로드.
    /// ManualQuantityRepo와 동일한 패턴, QuantitySettingsSchema 활용.
    /// </summary>
    public class RevitQuantitySettingsRepo : IQuantitySettingsRepository
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly Func<Document?> _doc;

        public RevitQuantitySettingsRepo(Func<Document?> doc)
        {
            _doc = doc;
        }

        public void Save(ProjectSettings settings)
        {
            var storage = GetOrCreateStorage();
            if (storage is null) return;

            var schema = QuantitySettingsSchema.GetOrCreate();
            var entity = new Entity(schema);
            entity.Set(QuantitySettingsSchema.FieldSettings, JsonSerializer.Serialize(settings, _jsonOptions));
            storage.SetEntity(entity);
        }

        public ProjectSettings? Load()
        {
            var storage = FindStorage();
            if (storage is null) return null;

            var schema = QuantitySettingsSchema.GetOrCreate();
            var entity = storage.GetEntity(schema);
            if (!entity.IsValid()) return null;

            var json = entity.Get<string>(QuantitySettingsSchema.FieldSettings);
            if (string.IsNullOrWhiteSpace(json)) return null;

            return JsonSerializer.Deserialize<ProjectSettings>(json, _jsonOptions);
        }

        private DataStorage? GetOrCreateStorage()
        {
            var doc = _doc();
            if (doc is null) return null;
            return FindStorage() ?? DataStorage.Create(doc);
        }

        private DataStorage? FindStorage()
        {
            var doc = _doc();
            if (doc is null) return null;
            var schema = QuantitySettingsSchema.GetOrCreate();
            return new FilteredElementCollector(doc)
                .OfClass(typeof(DataStorage))
                .Cast<DataStorage>()
                .FirstOrDefault(ds => ds.GetEntity(schema).IsValid());
        }
    }
}
