using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Structures;
using System;
using System.Linq;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit
{
    internal class RevitElementTypeCommandRepo : IElementTypeCommandRepo
    {
        private readonly Func<Document?> _doc;

        public RevitElementTypeCommandRepo(Func<Document?> doc)
        {
            _doc = doc;
        }
        
        public int FindOrCreateSlabType(FloorTypeSpec spec)
        {
            var doc = _doc();
            if (doc == null) return 0;

            var allFloorTypes = new FilteredElementCollector(doc)
                .OfClass(typeof(FloorType))
                .Cast<FloorType>()
                .ToList();

            var name = spec.Name;

            var existing = allFloorTypes.FirstOrDefault(ft => ft.Name == name);

            //Find: 지정한 Name의 FloorType이 이미 존재할 경우
            if (existing != null) return (int)existing.Id.Value;

            //Create: 지정한 Name 의 FloorType이 없을 경우
            var baseFloorType = new FilteredElementCollector(doc)
                .OfClass(typeof(FloorType))
                .Cast<FloorType>()
                .FirstOrDefault(ft => ft.GetCompoundStructure() != null);

            if (baseFloorType == null)
                TaskDialog.Show("Error", "적절한 복제 대상 FloorType이 없습니다");

            var newType = baseFloorType.Duplicate(name) as FloorType;
            if (newType == null) return 0;

            var cs = newType.GetCompoundStructure();

            var structureLayer = new CompoundStructureLayer( UC.MmToFt(spec.Thickness), MaterialFunctionAssignment.Structure, ElementId.InvalidElementId);
            cs.SetLayers(new List<CompoundStructureLayer> { structureLayer });
            // 코어 경계
            cs.SetNumberOfShellLayers(ShellLayerType.Exterior, 0);
            cs.SetNumberOfShellLayers(ShellLayerType.Interior, 0);

            newType.SetCompoundStructure(cs);

            return (int)newType.Id.Value;
        }

        public int FindOrCreateWallType(WallTypeSpec spec)
        {
            var doc = _doc();
            if (doc == null) return 0;

            var allWallTypes = new FilteredElementCollector(doc)
                .OfClass(typeof(WallType))
                .Cast<WallType>()
                .ToList();

            var name = spec.Name;

            var existing = allWallTypes.FirstOrDefault(wt => wt.Name == name);

            //Find: 지정한 Name의 WallType이 이미 존재할 경우
            if (existing != null) return (int)existing.Id.Value;

            //Create: 지정한 Name의 WallType이 없을 경우
            var baseWallType = new FilteredElementCollector(doc)
                .OfClass(typeof(WallType))
                .Cast<WallType>()
                .FirstOrDefault(ft => ft.GetCompoundStructure() != null);

            if (baseWallType == null)
                TaskDialog.Show("Error", "적절한 복제 대상 WallType이 없습니다");

            var newType = baseWallType.Duplicate(name) as WallType;
            if (newType == null) return 0;

            var cs = newType.GetCompoundStructure();

            var structureLayer = new CompoundStructureLayer(UC.MmToFt(spec.Thickness), MaterialFunctionAssignment.Structure, ElementId.InvalidElementId);
            cs.SetLayers(new List<CompoundStructureLayer> { structureLayer });
            // 코어 경계
            cs.SetNumberOfShellLayers(ShellLayerType.Exterior, 0);
            cs.SetNumberOfShellLayers(ShellLayerType.Interior, 0);

            newType.SetCompoundStructure(cs);

            return (int)newType.Id.Value;
        }

        public int FindOrCreateBeamType(BeamTypeSpec spec)
        {
            throw new NotImplementedException();
        }

    }
}
