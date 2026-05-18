using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;

namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class SheetDirectionStorageService
    {
        private readonly Document _doc;
        private static readonly Guid SchemaGuid = new("B8F3A211-4C9D-4E7F-8B2A-1D5E3F7C9B0A");
        private const string FieldName = "SheetDirection";

        public SheetDirectionStorageService(Document doc) { _doc = doc; }

        private static Schema GetOrCreateSchema()
        {
            var schema = Schema.Lookup(SchemaGuid);
            if (schema != null) return schema;

            var sb = new SchemaBuilder(SchemaGuid);
            sb.SetSchemaName("DHBIMWATER_SheetDirection");
            sb.SetReadAccessLevel(AccessLevel.Public);
            sb.SetWriteAccessLevel(AccessLevel.Public);
            sb.AddSimpleField(FieldName, typeof(string));
            return sb.Finish();
        }

        public void Save(string sheetId, string directionType)
        {
            if (!long.TryParse(sheetId, out var sid)) return;
            var sheet = _doc.GetElement(new ElementId(sid)) as ViewSheet;
            if (sheet == null) return;

            using var tx = new Transaction(_doc, "Save Sheet Direction");
            tx.Start();
            var schema = GetOrCreateSchema();
            var field = schema.GetField(FieldName);
            var ent = new Entity(schema);
            ent.Set(field, directionType ?? "Center");
            sheet.SetEntity(ent);
            tx.Commit();
        }

        public string Load(ViewSheet sheet)
        {
            if (sheet == null) return null;
            var schema = Schema.Lookup(SchemaGuid);
            if (schema == null) return null;

            var ent = sheet.GetEntity(schema);
            if (!ent.IsValid()) return null;

            return ent.Get<string>(schema.GetField(FieldName));
        }
    }
}
