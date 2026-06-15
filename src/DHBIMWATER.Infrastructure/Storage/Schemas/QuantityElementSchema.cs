using Autodesk.Revit.DB.ExtensibleStorage;
using System;

namespace DHBIMWATER.Infrastructure.Storage.Schemas
{
    internal static class QuantityElementSchema
    {
        private static readonly Guid SchemaGuid = new Guid("3F8A1C2D-4E7B-4F9A-8B3C-1D2E5F6A7B8C");
        private const string SchemaName = "DHBimWater_QuantityElement";
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
