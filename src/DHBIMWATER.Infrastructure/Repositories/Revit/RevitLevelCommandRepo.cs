using Autodesk.Revit.DB;
using DHBIMWATER.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Infrastructure.Repositories.Revit
{
    internal class RevitLevelCommandRepo : ILevelCommandRepo
    {

        private readonly Func<Document?> _doc;

        public RevitLevelCommandRepo(Func<Document?> doc)
        {
            _doc = doc;
        }

        public void CreateLevel(string levelName, double elevation)
        {
            var doc = _doc();
            if (doc == null) return;

            Level level = Level.Create(doc, elevation);
            level.Name = levelName;
        }

        public void UpdateLevel(string levelName, double elevation) 
        {
            var doc = _doc();
            if (doc == null) return;

            var level = new FilteredElementCollector(doc)
                          .OfCategory(BuiltInCategory.OST_Levels)
                          .WhereElementIsNotElementType()
                          .Cast<Level>()
                          .FirstOrDefault(lvl => lvl.Name.Equals(levelName, StringComparison.OrdinalIgnoreCase));
            if (level != null)
            {
                level.Elevation = elevation;
            }
        }
    }
}
