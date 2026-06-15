using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using DHBIMWATER.Application.Interfaces.Storage;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Infrastructure.Storage.Schemas;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Storage
{
    public class ManualQuantityRepo : IManualQuantityRepo
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly Document _doc;

        public ManualQuantityRepo(Document doc)
        {
            _doc = doc;
        }

        public void Save(IEnumerable<QuantityItem> items)
        {
            var storage = GetOrCreateStorage();
            var schema = ManualQuantityStorageSchema.GetOrCreate();
            var entity = new Entity(schema);
            entity.Set(ManualQuantityStorageSchema.FieldItems, JsonSerializer.Serialize(items, _jsonOptions));
            storage.SetEntity(entity);
        }

        public IReadOnlyList<QuantityItem> LoadAll()
        {
            var storage = FindStorage();
            if (storage is null) return [];

            var schema = ManualQuantityStorageSchema.GetOrCreate();
            var entity = storage.GetEntity(schema);
            if (!entity.IsValid()) return [];

            var json = entity.Get<string>(ManualQuantityStorageSchema.FieldItems);
            if (string.IsNullOrWhiteSpace(json)) return [];

            return JsonSerializer.Deserialize<List<QuantityItem>>(json, _jsonOptions) ?? [];
        }

        public void Clear()
        {
            var storage = FindStorage();
            if (storage is null) return;

            var schema = ManualQuantityStorageSchema.GetOrCreate();
            storage.DeleteEntity(schema);
        }

        private DataStorage GetOrCreateStorage()
        {
            return FindStorage() ?? DataStorage.Create(_doc);
        }

        private DataStorage? FindStorage()
        {
            var schema = ManualQuantityStorageSchema.GetOrCreate();
            return new FilteredElementCollector(_doc)
                .OfClass(typeof(DataStorage))
                .Cast<DataStorage>()
                .FirstOrDefault(ds => ds.GetEntity(schema).IsValid());
        }
    }
}
