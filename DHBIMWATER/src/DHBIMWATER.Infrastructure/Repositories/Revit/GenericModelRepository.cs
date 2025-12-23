using Autodesk.Revit.DB;
using DHBIMWATER.Application.Interface;

namespace DHBIMWATER.Infrastructure.Repositories.Revit
{
    internal class GenericModelRepository : IGenericModelRepository
    {
        private readonly Document _document;

        public GenericModelRepository(Document document)
        {
            _document = document;
        }

        public IEnumerable<object> GetAll()
        {
            return new FilteredElementCollector(_document)
                .OfCategory(BuiltInCategory.OST_GenericModel)   // Generic Model 카테고리 필터링
                .WhereElementIsNotElementType()                 // 요소 타입(ElementType) 제외
                .ToElements();                                  // 요소 컬렉션 반환
        }
    }
}
