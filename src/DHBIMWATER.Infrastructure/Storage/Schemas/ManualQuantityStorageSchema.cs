using Autodesk.Revit.DB.ExtensibleStorage;
using System;

namespace DHBIMWATER.Infrastructure.Storage.Schemas
{
    internal static class ManualQuantityStorageSchema
    {
        private static readonly Guid SchemaGuid = new Guid("7C4D9E1F-2A3B-4C5D-9E6F-0A1B2C3D4E5F");
        private const string SchemaName = "DHBimWater_ManualQuantityStorage";
        internal const string FieldItems = "Items";

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
            builder.AddSimpleField(FieldItems, typeof(string));
            return builder.Finish();
        }
    }
}
