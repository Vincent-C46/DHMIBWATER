using Autodesk.Revit.DB.ExtensibleStorage;
using System;

namespace DHBIMWATER.Infrastructure.Storage.Schemas
{
    internal static class QuantityRuleStorageSchema
    {
        private static readonly Guid SchemaGuid = new Guid("A3F5E7C9-1B2D-4F6E-8A0C-3D5E7F9A1B2C");
        private const string SchemaName = "DHBimWater_QuantityRuleStorage";
        internal const string FieldRuleSet = "RuleSet";

        internal static Schema GetOrCreate()
        {
            return Schema.Lookup(SchemaGuid) ?? Build();
        }

        private static Schema Build()
        {
            var builder = new SchemaBuilder(SchemaGuid);
            builder.SetSchemaName(SchemaName);
            builder.SetReadAccessLevel(AccessLevel.Public);
            builder.SetWriteAccessLevel(AccessLevel.Public);
            builder.AddSimpleField(FieldRuleSet, typeof(string));
            return builder.Finish();
        }
    }
}
