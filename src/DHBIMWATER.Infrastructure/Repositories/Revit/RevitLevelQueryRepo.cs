using Autodesk.Revit.DB;
using DHBIMWATER.Application.Interfaces;
using System.Collections.Generic;

namespace DHBIMWATER.Infrastructure.Repositories.Revit
{
    internal class RevitLevelQueryRepo : ILevelQueryRepo
    {
        private readonly Func<Document?> _doc;

        public RevitLevelQueryRepo(Func<Document?> doc)
        {
            _doc = doc;
        }

        public IEnumerable<string> GetExistingLevelNames()
        {
            var doc = _doc();
            if (doc == null) return new List<string>();
            
            var col = new FilteredElementCollector(doc)
                          .OfCategory(BuiltInCategory.OST_Levels)
                          .WhereElementIsNotElementType()
                          .Cast<Level>()
                          .ToList();

            var levelNames = col.Select(level => level.Name);

            return levelNames;
        }

        public IEnumerable<string> GetExistingPlanNames()
        {
            var doc = _doc();

            var viewPlanNames = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewPlan))
                .Cast<ViewPlan>()
                .Select(view => view.Name)
                .ToList();

            return viewPlanNames;
        }
    }
}
