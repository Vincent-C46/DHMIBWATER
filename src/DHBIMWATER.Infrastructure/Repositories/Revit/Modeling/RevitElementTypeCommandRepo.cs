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

        public int FindOrCreateConcreteMaterial(ConcreteSpec concrete)
        {
            var doc = _doc();
            if (doc == null) return 0;
            var allMaterials = new FilteredElementCollector(doc)
                .OfClass(typeof(Material))
                .Cast<Material>()
                .ToList();

            var strength = UnitUtils.ConvertToInternalUnits(concrete.CompressiveStrength, UnitTypeId.Megapascals);

            var existing = allMaterials.FirstOrDefault(m =>
                m.Name.Equals(concrete.MaterialName, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                // 이전(구조자산 미반영) 버전에서 생성돼 남아있는 재료일 수 있으므로, 강도가 다르면 갱신.
                ApplyConcreteCompression(doc, existing, concrete.MaterialName, strength);
                return (int)existing.Id.Value;
            }

            var baseMaterial = allMaterials.FirstOrDefault(m =>
                m.Name.IndexOf("concrete", StringComparison.OrdinalIgnoreCase) >= 0 ||
                m.Name.Contains("콘크리트"));

            if (baseMaterial == null)
                return 0;

            var newMat = baseMaterial.Duplicate(concrete.MaterialName) as Material;
            newMat.get_Parameter(BuiltInParameter.PHY_MATERIAL_PARAM_CONCRETE_COMPRESSION)?.Set(strength);
            ApplyConcreteCompression(doc, newMat, concrete.MaterialName, strength);

            return (int)newMat.Id.Value;
        }

        // 재료 이름뿐 아니라 구조자산(StructuralAsset)에도 강도 반영.
        // Duplicate() 직후에는 원본과 구조자산(PropertySetElement)을 공유하므로,
        // 값이 다를 때만 별도 자산으로 복제해서 재료에 연결한다(공유 중인 원본/라이브러리 자산을 직접 수정하지 않음).
        // ConcreteCompression은 StructuralAssetClass가 Concrete인 자산에만 유효(그 외 설정 시 예외 발생).
        private static void ApplyConcreteCompression(Document doc, Material material, string materialName, double strengthInternal)
        {
            if (doc.GetElement(material.StructuralAssetId) is not PropertySetElement pse) return;

            var asset = pse.GetStructuralAsset();
            if (asset == null || asset.StructuralAssetClass != StructuralAssetClass.Concrete) return;
            if (Math.Abs(asset.ConcreteCompression - strengthInternal) < 1e-9) return;

            asset.Name = $"{materialName} - 구조자산";
            asset.ConcreteCompression = strengthInternal;
            var newPse = PropertySetElement.Create(doc, asset);
            material.StructuralAssetId = newPse.Id;
        }

        public int FindOrCreateSlabType(FloorTypeSpec spec, int materialId)
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
            var structureLayer = new CompoundStructureLayer(UC.MmToFt(spec.Thickness), MaterialFunctionAssignment.Structure, new ElementId((long)materialId));
            cs.SetLayers(new List<CompoundStructureLayer> { structureLayer });
            cs.SetNumberOfShellLayers(ShellLayerType.Exterior, 0);
            cs.SetNumberOfShellLayers(ShellLayerType.Interior, 0);
            floorType.SetCompoundStructure(cs);

            return (int)floorType.Id.Value;
        }
        public int FindOrCreateWallType(WallTypeSpec spec, int materialId)
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
            var structureLayer = new CompoundStructureLayer(UC.MmToFt(spec.Thickness), MaterialFunctionAssignment.Structure, new ElementId((long)materialId));
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

        public int FindBeamSymbol(string typeName) => FindFamilySymbol(BuiltInCategory.OST_StructuralFraming, fs => fs.Name == typeName);

        public int FindHaunchBeamSymbol() => FindFamilySymbol(BuiltInCategory.OST_StructuralFraming,
            fs => fs.Name.Contains("헌치") || fs.Name.Contains("haunch") || fs.FamilyName.Contains("헌치") || fs.FamilyName.Contains("haunch"));

        public int FindColumnSymbol(string typeName) => FindFamilySymbol(BuiltInCategory.OST_StructuralColumns, fs => fs.Name == typeName);

        public int FindGenericModelSymbol(string symbolName) => FindFamilySymbol(BuiltInCategory.OST_GenericModel, fs => fs.Name.Contains(symbolName));

        private int FindFamilySymbol(BuiltInCategory category, Func<FamilySymbol, bool> predicate)
        {
            var doc = _doc();
            if (doc == null) return 0;
            return new FilteredElementCollector(doc)
                .OfCategory(category)
                .WhereElementIsElementType()
                .OfType<FamilySymbol>()
                .FirstOrDefault(predicate)?.Id.Value is long id ? (int)id : 0;
        }
    }
}
