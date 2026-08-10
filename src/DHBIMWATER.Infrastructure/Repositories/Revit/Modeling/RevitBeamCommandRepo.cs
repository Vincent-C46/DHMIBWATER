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

        public int CreateBeam(BeamDefinition beamDef, ConcreteSpec? concrete = null)
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
                var spec = new BeamTypeSpec(beamDef.Width, beamDef.Height, $"{beamDef.Width} x {beamDef.Height}", concrete);
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

            // HAUNCH와 밸브실 보(VR-B)만 Z맞춤을 명시 적용한다.
            // 그 외(PumpingStation GIRDER·Reservoir 등)는 기존대로 Revit 기본 Z맞춤을 사용한다.
            if (beamDef.Part == "HAUNCH" || beamDef.ElementCode == "VR-B")
                beam.get_Parameter(BuiltInParameter.Z_JUSTIFICATION).Set(beamDef.ZJustification);

            // 밸브실 보: 원점(2) 기준에서 보 높이의 절반만큼 아래로 내려 보 상단을 상부슬래브 상단(보 z)에 맞춘다.
            if (beamDef.ElementCode == "VR-B")
            {
                var halfHeightFt = GetBeamHeightFt(beamType) / 2;
                if (halfHeightFt > 0)
                    beam.get_Parameter(BuiltInParameter.Z_OFFSET_VALUE)?.Set(-halfHeightFt);
            }

            if (beamDef.ElementCode == "VR-B")
                JoinWithUpperSlab(beam);

            beam.LookupParameter("DH_ElementCode")?.Set(beamDef.ElementCode);
            beam.LookupParameter("DH_Addin")?.Set("DHBIMWATER");
            beam.LookupParameter("DH_Part")?.Set(beamDef.Part);
            beam.LookupParameter("DH_Zone")?.Set(beamDef.Zone);
            beam.LookupParameter("DH_Category")?.Set(beamDef.Part == "HAUNCH" ? "헌치" : beamDef.Category);

            //haunch.get_Parameter(BuiltInParameter.Z_JUSTIFICATION).Set(2);  // Z맞춤: 상단(0), 중심(1), 원점 (2), 하단(3)

            return (int)beam.Id.Value;
        }

        /// <summary>보 타입의 단면 높이를 내부 단위(피트)로 반환한다. 단면 높이 파라미터가 없으면 0.</summary>
        private static double GetBeamHeightFt(FamilySymbol beamType)
        {
            var p = beamType.get_Parameter(BuiltInParameter.STRUCTURAL_SECTION_COMMON_HEIGHT)
                    ?? beamType.LookupParameter("h")
                    ?? beamType.LookupParameter("H");
            return p?.AsDouble() ?? 0;
        }

        private void JoinWithUpperSlab(Element beam)
        {
            var doc = _doc();
            doc.Regenerate();
            var beamBoundingBox = beam.get_BoundingBox(null);
            var upperSlabs = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Floors)
                .WhereElementIsNotElementType()
                .Cast<Floor>()
                .Where(floor => BoundingBoxesOverlap(beamBoundingBox, floor.get_BoundingBox(null)))
                .ToList();

            if (upperSlabs.Count == 0)
            {
                TaskDialog.Show("Error", $"제수밸브실 보와 겹치는 슬래브를 찾을 수 없습니다. (보: {beam.Id.Value})");
                return;
            }

            foreach (var slab in upperSlabs)
            {
                try
                {
                    if (JoinGeometryUtils.AreElementsJoined(doc, slab, beam)) continue;
                    JoinGeometryUtils.JoinGeometry(doc, slab, beam);
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Error", $"제수밸브실 보와 상부슬래브를 결합하지 못했습니다. (보: {beam.Id.Value}, 슬래브: {slab.Id.Value})\n{ex.Message}");
                }
            }
        }

        private static bool BoundingBoxesOverlap(BoundingBoxXYZ? first, BoundingBoxXYZ? second)
        {
            if (first == null || second == null) return false;

            return first.Min.X <= second.Max.X && first.Max.X >= second.Min.X
                && first.Min.Y <= second.Max.Y && first.Max.Y >= second.Min.Y
                && first.Min.Z <= second.Max.Z && first.Max.Z >= second.Min.Z;
        }
    }
}
