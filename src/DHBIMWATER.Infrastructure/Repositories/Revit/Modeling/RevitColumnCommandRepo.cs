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

        public int PlaceColumn(ColumnDefinition def)
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

            var typeName = $"{(int)def.Width} x {(int)def.Depth}";

            var allColumnTypes = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_StructuralColumns)
                .WhereElementIsElementType()
                .Cast<FamilySymbol>()
                .Where(fs => fs.Family.StructuralMaterialType == StructuralMaterialType.Concrete)
                .ToList();

            var colType = allColumnTypes.FirstOrDefault(fs => fs.Name == typeName);

            if (colType == null)
            {
                var baseType = allColumnTypes.FirstOrDefault(fs =>
                    fs.LookupParameter("b") != null && fs.LookupParameter("h") != null);

                if (baseType == null)
                {
                    TaskDialog.Show("Error", "적절한 복제 대상 Column type이 없습니다.");
                    return 0;
                }

                colType = baseType.Duplicate(typeName) as FamilySymbol;
                colType.LookupParameter("b")?.Set(UC.MmToFt(def.Width));
                colType.LookupParameter("h")?.Set(UC.MmToFt(def.Depth));
            }

            if (!colType.IsActive)
            {
                colType.Activate();
                doc.Regenerate();
            }

            var insertPt = new XYZ(UC.MmToFt(def.Position.X), UC.MmToFt(def.Position.Y), UC.MmToFt(def.Position.Z));
            var col = doc.Create.NewFamilyInstance(insertPt, colType, level, StructuralType.Column);

            col.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM)?.Set(UC.MmToFt(def.Height));

            col.LookupParameter("DH_ElementCode")?.Set(def.ElementCode);
            col.LookupParameter("DH_Addin")?.Set("DHBIMWATER");
            col.LookupParameter("DH_Part")?.Set(def.Part);
            col.LookupParameter("DH_Zone")?.Set(def.Zone);
            col.LookupParameter("DH_Category")?.Set(def.Category);

            return (int)col.Id.Value;
        }
    }
}
