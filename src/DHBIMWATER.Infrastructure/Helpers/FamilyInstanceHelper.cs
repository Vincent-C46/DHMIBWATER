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

            var typeParam = element.Document
                .GetElement(element.GetTypeId())
                ?.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM)
                ?.AsElementId();
            if (typeParam != null && typeParam != ElementId.InvalidElementId)
                return typeParam;

            // 사용자 정의 재료 파라미터 탐색: 참조 요소가 Material인 첫 번째 파라미터
            return element.Parameters
                .Cast<Parameter>()
                .Where(p => p.StorageType == StorageType.ElementId)
                .Select(p => p.AsElementId())
                .FirstOrDefault(id => id != null
                                   && id != ElementId.InvalidElementId
                                   && element.Document.GetElement(id) is Material);
        }
        public static StructuralAssetClass? GetStructuralAssetClass(Element element)
        {
            var materialId = GetMaterialId(element);
            if (materialId == null) return null;

            var material = element.Document.GetElement(materialId) as Material;
            if (material == null) return null;

            var assetId = material.StructuralAssetId;
            if (assetId == null || assetId == ElementId.InvalidElementId) return null;

            var pse = element.Document.GetElement(assetId) as PropertySetElement;
            return pse?.GetStructuralAsset()?.StructuralAssetClass;
        }

        public static bool IsConcreteMaterial(Document doc, ElementId? materialId)
        {
            if (materialId == null || materialId == ElementId.InvalidElementId) return false;
            var material = doc.GetElement(materialId) as Material;
            if (material == null) return false;

            var assetId = material.StructuralAssetId;
            if (assetId == null || assetId == ElementId.InvalidElementId) return false;

            var cls = (doc.GetElement(assetId) as PropertySetElement)
                          ?.GetStructuralAsset()?.StructuralAssetClass;
            return cls == StructuralAssetClass.Concrete;
        }
    }
}