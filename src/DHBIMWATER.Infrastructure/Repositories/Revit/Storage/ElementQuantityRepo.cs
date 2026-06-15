using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using DHBIMWATER.Application.Interfaces.Storage;
using DHBIMWATER.Core.Quantity;
using DHBIMWATER.Infrastructure.Storage.Schemas;
using System.Collections.Generic;
using System.Text.Json;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Storage
{
    public class ElementQuantityRepo : IElementQuantityRepo
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly Func<Document?> _doc;

        public ElementQuantityRepo(Func<Document?> doc)
        {
            _doc = doc;
        }

        public void Save(long elementId, IEnumerable<QuantityItem> items)
        {
            var element = GetElement(elementId);
            if (element is null) return;

            var schema = QuantityElementSchema.GetOrCreate();
            var entity = new Entity(schema);
            entity.Set(QuantityElementSchema.FieldItems, JsonSerializer.Serialize(items, _jsonOptions));
            element.SetEntity(entity);
        }

        public IReadOnlyList<QuantityItem> Load(long elementId)
        {
            var element = GetElement(elementId);
            if (element is null) return [];

            var schema = QuantityElementSchema.GetOrCreate();
            var entity = element.GetEntity(schema);
            if (!entity.IsValid()) return [];

            var json = entity.Get<string>(QuantityElementSchema.FieldItems);
            if (string.IsNullOrWhiteSpace(json)) return [];

            return JsonSerializer.Deserialize<List<QuantityItem>>(json, _jsonOptions) ?? [];
        }

        public void Delete(long elementId)
        {
            var element = GetElement(elementId);
            if (element is null) return;

            var schema = QuantityElementSchema.GetOrCreate();
            element.DeleteEntity(schema);
        }

        public bool HasData(long elementId)
        {
            var element = GetElement(elementId);
            if (element is null) return false;

            var schema = QuantityElementSchema.GetOrCreate();
            return element.GetEntity(schema).IsValid();
        }

        private Element? GetElement(long elementId) =>
            _doc()?.GetElement(new ElementId(elementId));
    }
}
