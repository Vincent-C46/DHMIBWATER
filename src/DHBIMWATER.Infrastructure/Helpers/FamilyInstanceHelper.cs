using Autodesk.Revit.DB;

namespace DHBIMWATER.Infrastructure.Helpers
{
    public static class FamilyInstanceHelper
    {
        public static double? FindParameter(FamilyInstance fi, string parameterName)
        {
            var lower = parameterName.ToLower();

            var param = fi.Parameters
                          .OfType<Parameter>()
                          .FirstOrDefault(p => p.Definition.Name.ToLower() == lower);
            if (param != null && param.HasValue) return param.AsDouble();

            var symbol = fi.Symbol;
            if (symbol == null) return null;

            param = symbol.Parameters
                          .OfType<Parameter>()
                          .FirstOrDefault(p => p.Definition.Name.ToLower() == lower);

            return param?.AsDouble();
        }


        public static ElementId? GetMaterialId(Element element)
        {
            var instanceParam = element
                .get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM)
                ?.AsElementId();

            if (instanceParam != null && instanceParam != ElementId.InvalidElementId)
                return instanceParam;

            return element.Document
                .GetElement(element.GetTypeId())
                ?.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM)
                ?.AsElementId();
        }
    }
}