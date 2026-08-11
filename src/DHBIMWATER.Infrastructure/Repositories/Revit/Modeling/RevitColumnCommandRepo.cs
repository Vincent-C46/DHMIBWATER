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

        public int CreateColumn(ColumnDefinition def, long baseLevelId, long topLevelId, int columnTypeId)
        {
            var doc = _doc();
            if (doc == null) return 0;

            var baseLevel = doc.GetElement(new ElementId(baseLevelId)) as Level;
            var topLevel = doc.GetElement(new ElementId(topLevelId)) as Level;

            if (baseLevel == null || topLevel == null)
            {
                TaskDialog.Show("Error", $"기둥 레벨을 찾을 수 없습니다: {def.BaseLevelName} or {def.TopLevelName}");
                return 0;
            }

            var colType = doc.GetElement(new ElementId((long)columnTypeId)) as FamilySymbol;

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
            var col = doc.Create.NewFamilyInstance(basePt, colType, baseLevel, StructuralType.Column);

            col.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM)?.Set(topLevel.Id);
            col.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM)?.Set(UC.MmToFt(def.TopOffset));
            col.LookupParameter("DH_ElementCode")?.Set(def.ElementCode);
            col.LookupParameter("DH_Addin")?.Set("DHBIMWATER");
            col.LookupParameter("DH_Part")?.Set(def.Part);
            col.LookupParameter("DH_Zone")?.Set(def.Zone);

            return (int)col.Id.Value;   
        }
    }
}
