using Autodesk.Revit.DB;
using DHBIMWATER.Application.Interfaces;
using System.Collections.Generic;
using UC = DHBIMWATER.Shared.Helpers.UnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Modeling
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

        public IReadOnlyDictionary<string, long> GetLevelIds(IEnumerable<string> levelNames)
        {
            var doc = _doc();
            if (doc == null) return new Dictionary<string, long>();

            var names = levelNames.ToHashSet(StringComparer.Ordinal);
            return new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .Where(level => names.Contains(level.Name))
                .ToDictionary(level => level.Name, level => level.Id.Value, StringComparer.Ordinal);
        }

        public IEnumerable<string> GetExistingPlanNames()
        {
            var doc = _doc();

            List<string> existingEngineeringPlanNames = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewPlan))
                .Cast<ViewPlan>()
                .Where(vp => vp.ViewType == ViewType.EngineeringPlan)
                .Select(vp => vp.Name)
                .ToList();

            return existingEngineeringPlanNames;
        }

        public IEnumerable<string> GetExistingSectionNames()
        {
            var doc = _doc();

            List<string> existingSectionViewNames = new FilteredElementCollector(doc)
                                                        .OfClass(typeof(ViewSection))
                                                        .WhereElementIsNotElementType()
                                                        .Cast<ViewSection>()
                                                        .Select(vs => vs.Name)
                                                        .ToList();

            return existingSectionViewNames;
        }

    }
}
