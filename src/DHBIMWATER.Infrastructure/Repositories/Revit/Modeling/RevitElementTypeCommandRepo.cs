using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Structures;
using System;
using System.Linq;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Modeling
{
    internal class RevitElementTypeCommandRepo : IElementTypeCommandRepo
    {
        private readonly Func<Document?> _doc;

        public RevitElementTypeCommandRepo(Func<Document?> doc)
        {
            _doc = doc;
        }

        private ElementId FindOrCreateConcreteMaterial(Document doc, ConcreteSpec concrete)
        {
            var allMaterials = new FilteredElementCollector(doc)
                .OfClass(typeof(Material))
                .Cast<Material>()
                .ToList();

            var existing = allMaterials.FirstOrDefault(m =>
                m.Name.Equals(concrete.MaterialName, StringComparison.OrdinalIgnoreCase));
            if (existing != null) return existing.Id;

            var baseMaterial = allMaterials.FirstOrDefault(m =>
                m.Name.IndexOf("concrete", StringComparison.OrdinalIgnoreCase) >= 0 ||
                m.Name.Contains("콘크리트"));

            if (baseMaterial == null)
                return ElementId.InvalidElementId;

            var newMat = baseMaterial.Duplicate(concrete.MaterialName) as Material;
            var strength = UnitUtils.ConvertToInternalUnits(concrete.CompressiveStrength, UnitTypeId.Megapascals);
            newMat.get_Parameter(BuiltInParameter.PHY_MATERIAL_PARAM_CONCRETE_COMPRESSION)?.Set(strength);
            return newMat.Id;
        }

        public int FindOrCreateSlabType(FloorTypeSpec spec)
        {
            var doc = _doc();
            if (doc == null) return 0;

            var name = spec.Name;

            var floorType = new FilteredElementCollector(doc)
                .OfClass(typeof(FloorType))
                .Cast<FloorType>()
                .FirstOrDefault(ft => ft.Name == name);

            if (floorType == null)
            {
                var baseFloorType = new FilteredElementCollector(doc)
                    .OfClass(typeof(FloorType))
                    .Cast<FloorType>()
                    .FirstOrDefault(ft => ft.GetCompoundStructure() != null);

                if (baseFloorType == null)
                {
                    TaskDialog.Show("Error", "적절한 복제 대상 FloorType이 없습니다");
                    return 0;
                }

                floorType = baseFloorType.Duplicate(name) as FloorType;
                if (floorType == null) return 0;
            }

            var cs = floorType.GetCompoundStructure();
            var materialId = spec.Concrete != null
                ? FindOrCreateConcreteMaterial(doc, spec.Concrete)
                : ElementId.InvalidElementId;
            var structureLayer = new CompoundStructureLayer(UC.MmToFt(spec.Thickness), MaterialFunctionAssignment.Structure, materialId);
            cs.SetLayers(new List<CompoundStructureLayer> { structureLayer });
            cs.SetNumberOfShellLayers(ShellLayerType.Exterior, 0);
            cs.SetNumberOfShellLayers(ShellLayerType.Interior, 0);
            floorType.SetCompoundStructure(cs);

            return (int)floorType.Id.Value;
        }
        public int FindOrCreateWallType(WallTypeSpec spec)
        {
            var doc = _doc();
            if (doc == null) return 0;

            var name = spec.Name;

            var wallType = new FilteredElementCollector(doc)
                .OfClass(typeof(WallType))
                .Cast<WallType>()
                .FirstOrDefault(wt => wt.Name == name);

            if (wallType == null)
            {
                var baseWallType = new FilteredElementCollector(doc)
                    .OfClass(typeof(WallType))
                    .Cast<WallType>()
                    .FirstOrDefault(wt => wt.GetCompoundStructure() != null);

                if (baseWallType == null)
                {
                    TaskDialog.Show("Error", "적절한 복제 대상 WallType이 없습니다");
                    return 0;
                }

                wallType = baseWallType.Duplicate(name) as WallType;
                if (wallType == null) return 0;
            }

            var cs = wallType.GetCompoundStructure();
            var materialId = spec.Concrete != null
                ? FindOrCreateConcreteMaterial(doc, spec.Concrete)
                : ElementId.InvalidElementId;
            var structureLayer = new CompoundStructureLayer(UC.MmToFt(spec.Thickness), MaterialFunctionAssignment.Structure, materialId);
            cs.SetLayers(new List<CompoundStructureLayer> { structureLayer });
            cs.SetNumberOfShellLayers(ShellLayerType.Exterior, 0);
            cs.SetNumberOfShellLayers(ShellLayerType.Interior, 0);
            wallType.SetCompoundStructure(cs);

            return (int)wallType.Id.Value;
        }

        public int FindOrCreateBeamType(BeamTypeSpec spec)
        {
            var doc = _doc();
            if (doc == null) return 0;

            var allBeamTypes = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_StructuralFraming)
                .OfClass(typeof(FamilySymbol))
                .WhereElementIsElementType()
                .Cast<FamilySymbol>()
                .Where(fs => fs.Family.StructuralMaterialType == StructuralMaterialType.Concrete)
                .ToList();

            // Find
            var existing = allBeamTypes.FirstOrDefault(b => b.Name.Contains(spec.Name));
            if (existing != null) return (int)existing.Id.Value;

            //Create
            var baseType = allBeamTypes.FirstOrDefault(fs => fs.LookupParameter("b") != null && fs.LookupParameter("h") != null);

            if (baseType == null) {
                TaskDialog.Show("Error", "적절한 복제 대상 Beam type이 없습니다.");
                return 0;
            }

            var newType = baseType.Duplicate(spec.Name) as FamilySymbol;
            newType.LookupParameter("b")?.Set(UC.MmToFt(spec.Width));
            newType.LookupParameter("h")?.Set(UC.MmToFt(spec.Height));

            if (!newType.IsActive)
            {
                newType.Activate();
                doc.Regenerate();
            }

            return (int)newType.Id.Value;
        }
    }
}
