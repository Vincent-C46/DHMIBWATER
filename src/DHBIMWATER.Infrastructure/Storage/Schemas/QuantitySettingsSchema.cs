using Autodesk.Revit.DB.ExtensibleStorage;
using System;

namespace DHBIMWATER.Infrastructure.Storage.Schemas
{
    internal static class QuantitySettingsSchema
    {
        private static readonly Guid SchemaGuid = new Guid("B1E2F3A4-5C6D-7E8F-A9B0-C1D2E3F4A5B6");
        private const string SchemaName = "DHBimWater_QuantitySettings";
        internal const string FieldSettings = "Settings";

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
            builder.AddSimpleField(FieldSettings, typeof(string));
            return builder.Finish();
        }
    }
}
