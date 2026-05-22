using Autodesk.Revit.DB;
using DHBIMWATER.Application.DTOs.Revit.PumpingStation;
using DHBIMWATER.Application.Interfaces;

namespace DHBIMWATER.Infrastructure.Repositories.Revit
{
    internal class RevitSetParameterRepo : ISetParameterRepo
    {
        private readonly Func<Document?> _doc;
        public RevitSetParameterRepo(Func<Document?> doc)
        {
            _doc = doc;
        }

        public void SetTypeParameter(PumpCreationRequestDto dto)
        {
            Document? doc = _doc(); 
            if (doc == null) return;

            var categories = new List<BuiltInCategory>
            {
                BuiltInCategory.OST_Walls,
                BuiltInCategory.OST_Floors,
                BuiltInCategory.OST_StructuralColumns,
                BuiltInCategory.OST_StructuralFraming,
                BuiltInCategory.OST_StructuralFoundation,
                BuiltInCategory.OST_Stairs,
                BuiltInCategory.OST_GenericModel
            };
            var filter = new ElementMulticategoryFilter(categories);

            var elems = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .WherePasses(filter)
                .ToElements();

            foreach(var elem in elems)
                elem.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS)?.Set($"{dto.DesignConditionDto.SelectedPumpingStationType}_{dto.DesignConditionDto.SelectedEntranceType}");
        }
    }
}
