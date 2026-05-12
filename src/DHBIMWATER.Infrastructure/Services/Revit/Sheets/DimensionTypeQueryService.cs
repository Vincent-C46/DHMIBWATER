using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using DHBIMWATER.Application.DTOs.Revit.Sheets;


namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class DimensionTypeQueryService
    {
        private readonly Document _doc;

        public DimensionTypeQueryService(Document doc)
        {
            _doc = doc;
        }

        public IList<DimensionTypeDto> GetDimensionTypes()
        {
            return new FilteredElementCollector(_doc)
                .OfClass(typeof(DimensionType))
                .Cast<DimensionType>()
                .Where(x => x.StyleType == DimensionStyleType.Linear)
                .Select(x => new DimensionTypeDto
                {
                    Id = x.Id.Value.ToString(),
                    Name = x.Name
                })
                .OrderBy(x => x.Name)
                .ToList();
        }
    }
}
