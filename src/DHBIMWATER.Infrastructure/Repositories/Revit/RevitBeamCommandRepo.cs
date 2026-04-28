using DHBIMWATER.Application.Interfaces;
using Autodesk.Revit.DB;
using DHBIMWATER.Core.Structures;
using Autodesk.Revit.DB.Structure;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;
using System.Windows.Controls;

namespace DHBIMWATER.Infrastructure.Repositories.Revit
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
                        
            var elementId = 0;

            var beamTyoeSpec = new BeamTypeSpec(beamDef.Width, beamDef.Height, $"{beamDef.Width} x {beamDef.Height}");
            int beamTypeId = _elementTypeCmdRepo.FindOrCreateBeamType(beamTyoeSpec);

            var beamType = doc.GetElement(new ElementId(beamTypeId)) as FamilySymbol;

            var curve = Line.CreateBound(new XYZ(UC.MmToFt(beamDef.StartPoint.X), UC.MmToFt(beamDef.StartPoint.Y), UC.MmToFt(beamDef.StartPoint.Z)),
                                         new XYZ(UC.MmToFt(beamDef.EndPoint.X), UC.MmToFt(beamDef.EndPoint.Y), UC.MmToFt(beamDef.EndPoint.Z)));
            var levelId = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .FirstOrDefault(e => e.Name.Equals(beamDef.LevelName))?.Id ?? ElementId.InvalidElementId;
            Level level = doc.GetElement(levelId) as Level;

            var beam = doc.Create.NewFamilyInstance(curve, beamType, level, StructuralType.Beam);
            StructuralFramingUtils.DisallowJoinAtEnd(beam, 0);
            StructuralFramingUtils.DisallowJoinAtEnd(beam, 1);

            beam.get_Parameter(BuiltInParameter.Z_JUSTIFICATION).Set(beamDef.Zjustification);

            return (int)beam.Id.Value;
        }
    }
}
