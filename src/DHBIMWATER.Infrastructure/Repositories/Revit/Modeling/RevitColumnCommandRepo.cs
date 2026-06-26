using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Structures;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Modeling
{
    public class RevitColumnCommandRepo : IColumnCommandRepo
    {
        private readonly Func<Document?> _doc;

        public RevitColumnCommandRepo(Func<Document?> doc)
        {
            _doc = doc;
        }

        public int CreateColumn(ColumnDefinition def)
        {
            var doc = _doc();
            if (doc == null) return 0;

            var level = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .FirstOrDefault(l => l.Name == def.LevelName);

            if (level == null)
            {
                TaskDialog.Show("Error", $"기둥 레벨을 찾을 수 없습니다: {def.LevelName}");
                return 0;
            }

            var colType = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_StructuralColumns)
                .WhereElementIsElementType()
                .Cast<FamilySymbol>()
                .FirstOrDefault(fs => fs.Name == def.TypeName);

            if (colType == null)
            {
                TaskDialog.Show("Error", $"기둥 유형을 찾을 수 없습니다: {def.TypeName}");
                return 0;
            }

            if (!colType.IsActive)
            {
                colType.Activate();
                doc.Regenerate();
            }

            var basePt = new XYZ(UC.MmToFt(def.Position.X), UC.MmToFt(def.Position.Y), UC.MmToFt(def.Position.Z));
            var col = doc.Create.NewFamilyInstance(basePt, colType, level, StructuralType.Column);

            col.LookupParameter("DH_ElementCode")?.Set(def.ElementCode);
            col.LookupParameter("DH_Addin")?.Set("DHBIMWATER");
            col.LookupParameter("DH_Part")?.Set(def.Part);
            col.LookupParameter("DH_Zone")?.Set(def.Zone);
            col.LookupParameter("DH_Category")?.Set(def.Category);

            return (int)col.Id.Value;   
        }
    }
}
