using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using DHBIMWATER.Application.DTOs.Revit;

namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class TitleBlockQueryService
    {
        private readonly Document _doc;
        public TitleBlockQueryService(Document doc) { _doc = doc; }

        public IList<TitleBlockDto> GetTitleBlocks()
        {
            return new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .WhereElementIsElementType()
                .Cast<ElementType>()
                .Select(t => new TitleBlockDto
                {
                    Id = t.Id.Value.ToString(),
                    DisplayName = t.Name
                })
                .ToList();
        }
    }
}
