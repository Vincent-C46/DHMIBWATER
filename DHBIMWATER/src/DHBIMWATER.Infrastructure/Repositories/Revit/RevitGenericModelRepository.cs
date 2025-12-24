using Autodesk.Revit.DB;
using DHBIMWATER.Application.Interface;

namespace DHBIMWATER.Infrastructure.Repositories.Revit
{
    internal class RevitGenericModelRepository : IGenericModelRepository
    {
        private readonly Func<Document?> _doc;

        public RevitGenericModelRepository(Func<Document?> doc)
        {
            _doc = doc;
        }

        public IEnumerable<object> GetAll()
        {
            Document? doc = _doc(); // 서비스에 주입된 람다식을 호출함으로써 UIApplication의 현재 Document를 가져옴
            if(doc == null) return Enumerable.Empty<object>();

            return new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_GenericModel)   // Generic Model 카테고리 필터링
                .WhereElementIsNotElementType()                 // 요소 타입(ElementType) 제외
                .ToElements();                                  // 요소 컬렉션 반환
        }
    }
}
