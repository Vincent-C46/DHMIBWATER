using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Structures;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Modeling
{
    public class RevitBeamCommandRepo : IBeamCommandRepo
    {
        private readonly Func<Document?> _doc;
        private readonly IElementTypeCommandRepo _elementTypeCmdRepo;

        public RevitBeamCommandRepo(Func<Document?> doc, IElementTypeCommandRepo elementTypeRepo)
        {
            _doc = doc;
            _elementTypeCmdRepo = elementTypeRepo;
        }

        public int CreateBeam(BeamDefinition beamDef)
        {
            var doc = _doc();
            if (doc == null) return 0;

            FamilySymbol? beamType;

            if (beamDef.Part == "HAUNCH")
            {
                beamType = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_StructuralFraming)
                    .WhereElementIsElementType()
                    .Cast<FamilySymbol>()
                    .FirstOrDefault(s => s.Name.Contains("헌치") || s.Name.Contains("haunch") || s.FamilyName.Contains("헌치") || s.FamilyName.Contains("haunch"));

                if (beamType == null)
                {
                    //TaskDialog.Show("Error", "헌치 패밀리 심볼을 찾을 수 없습니다.");
                    return 0;
                }
            }
            else if (!string.IsNullOrEmpty(beamDef.TypeName))
            {
                beamType = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_StructuralFraming)
                    .WhereElementIsElementType()
                    .Cast<FamilySymbol>()
                    .FirstOrDefault(fs => fs.Name == beamDef.TypeName);

                if (beamType == null)
                {
                    TaskDialog.Show("Error", $"보 유형을 찾을 수 없습니다: {beamDef.TypeName}");
                    return 0;
                }
            }
            else
            {
                var spec = new BeamTypeSpec(beamDef.Width, beamDef.Height, $"{beamDef.Width} x {beamDef.Height}");
                int typeId = _elementTypeCmdRepo.FindOrCreateBeamType(spec);
                beamType = doc.GetElement(new ElementId((long)typeId)) as FamilySymbol;
                if (beamType == null) return 0;
            }

            if (!beamType.IsActive)
            {
                beamType.Activate();
                //TaskDialog.Show("Info", $"Beam type '{beamType.Name}' activated. BeamRepo에서 활성화됨");
            }

            var curve = Line.CreateBound(new XYZ(UC.MmToFt(beamDef.StartPoint.X), UC.MmToFt(beamDef.StartPoint.Y), UC.MmToFt(beamDef.StartPoint.Z)),
                                         new XYZ(UC.MmToFt(beamDef.EndPoint.X), UC.MmToFt(beamDef.EndPoint.Y), UC.MmToFt(beamDef.EndPoint.Z)));
            var levelId = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .FirstOrDefault(e => e.Name.Equals(beamDef.LevelName))?.Id ?? ElementId.InvalidElementId;
            Level level = doc.GetElement(levelId) as Level;

            var beam = doc.Create.NewFamilyInstance(curve, beamType, level, StructuralType.Beam);
            StructuralFramingUtils.DisallowJoinAtEnd(beam, 0);
            StructuralFramingUtils.DisallowJoinAtEnd(beam, 1);

            beam.get_Parameter(BuiltInParameter.Z_JUSTIFICATION).Set(beamDef.ZJustification);
            JoinWithSlab(beam);

            beam.LookupParameter("DH_ElementCode")?.Set(beamDef.ElementCode);
            beam.LookupParameter("DH_Addin")?.Set("DHBIMWATER");
            beam.LookupParameter("DH_Part")?.Set(beamDef.Part);
            beam.LookupParameter("DH_Zone")?.Set(beamDef.Zone);
            beam.LookupParameter("DH_Category")?.Set(beamDef.Part == "HAUNCH" ? "헌치" : beamDef.Category);

            return (int)beam.Id.Value;
        }

        private void JoinWithSlab(Element beam)
        {
            var doc = _doc();
            doc.Regenerate();   
            var intersectFilter = new ElementIntersectsElementFilter(beam);
            var intersectSlabs = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Floors)
                .WhereElementIsNotElementType()
                .WherePasses(intersectFilter)
                .ToElements()
                .ToList();

            if (intersectSlabs.Count == 0) return;

            foreach (var slab in intersectSlabs)
            {
                try
                {
                    JoinGeometryUtils.JoinGeometry(doc, slab, beam);
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Error", ex.Message);
                }
            }
            return;
        }
    }
}
