using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using DHBIMWATER.Application.Interfaces.Quantity;
using DHBIMWATER.Core.Quantity.RuleSets;
using DHBIMWATER.Infrastructure.Storage.Schemas;
using System;
using System.Linq;
using System.Text.Json;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Storage
{
    public class RevitQuantityRuleRepo : IQuantityRuleRepository
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly Func<Document?> _doc;

        public RevitQuantityRuleRepo(Func<Document?> doc)
        {
            _doc = doc;
        }

        public RuleSet? GetProjectRuleSet()
        {
            var storage = FindStorage();
            if (storage is null) return null;

            var schema = QuantityRuleStorageSchema.GetOrCreate();
            var entity = storage.GetEntity(schema);
            if (!entity.IsValid()) return null;

            var json = entity.Get<string>(QuantityRuleStorageSchema.FieldRuleSet);
            if (string.IsNullOrWhiteSpace(json)) return null;

            return JsonSerializer.Deserialize<RuleSet>(json, _jsonOptions);
        }

        public void SaveProjectRuleSet(RuleSet ruleSet)
        {
            var storage = GetOrCreateStorage();
            if (storage is null) return;

            var schema = QuantityRuleStorageSchema.GetOrCreate();
            var entity = new Entity(schema);
            entity.Set(QuantityRuleStorageSchema.FieldRuleSet, JsonSerializer.Serialize(ruleSet, _jsonOptions));
            storage.SetEntity(entity);
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
            var schema = QuantityRuleStorageSchema.GetOrCreate();
            return new FilteredElementCollector(doc)
                .OfClass(typeof(DataStorage))
                .Cast<DataStorage>()
                .FirstOrDefault(ds => ds.GetEntity(schema).IsValid());
        }
    }
}
