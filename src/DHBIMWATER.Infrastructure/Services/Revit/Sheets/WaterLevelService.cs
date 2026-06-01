using System;
using System.Globalization;
using System.Linq;
using Autodesk.Revit.DB;

namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class WaterLevelService
    {
        private readonly Document _doc;

        public WaterLevelService(Document doc)
        {
            _doc = doc;
        }

        public void CreateOrUpdate(string hwlStr, string lwlStr)
        {
            var hwlFeet = ParseMetersToFeet(hwlStr);
            var lwlFeet = ParseMetersToFeet(lwlStr);

            using var tx = new Transaction(_doc, "Create Water Levels");
            tx.Start();

            if (hwlFeet.HasValue) ApplyLevel("HWL", hwlFeet.Value);
            if (lwlFeet.HasValue) ApplyLevel("LWL", lwlFeet.Value);

            HideNonWaterLevelsOnSectionViews();

            tx.Commit();
        }

        public void HideNonWaterLevels()
        {
            using var tx = new Transaction(_doc, "Hide Non-Water Levels");
            tx.Start();
            HideNonWaterLevelsOnSectionViews();
            tx.Commit();
        }

        private void HideNonWaterLevelsOnSectionViews()
        {
            var waterLevelIds = GetWaterLevelIds();

            var sectionViews = new FilteredElementCollector(_doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v => v is ViewSection &&
                            v.Name.EndsWith("_시트", StringComparison.OrdinalIgnoreCase) &&
                            !v.IsTemplate)
                .ToList();

            var allLevels = new FilteredElementCollector(_doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .ToList();

            foreach (var view in sectionViews)
            {
                var toHide = allLevels
                    .Where(l => !waterLevelIds.Contains(l.Id) && l.CanBeHidden(view))
                    .Select(l => l.Id)
                    .ToList();

                if (toHide.Count > 0)
                    view.HideElements(toHide);

                var toUnhide = waterLevelIds
                    .Where(id => _doc.GetElement(id) is Level l && l.IsHidden(view))
                    .ToList();

                if (toUnhide.Count > 0)
                    view.UnhideElements(toUnhide);

                foreach (var id in waterLevelIds)
                {
                    if (_doc.GetElement(id) is Level wl)
                        TrimLevelInView(wl, view);
                }
            }
        }

        private void TrimLevelInView(Level level, View view)
        {
            try
            {
                level.SetDatumExtentType(DatumEnds.End0, view, DatumExtentType.ViewSpecific);
                level.SetDatumExtentType(DatumEnds.End1, view, DatumExtentType.ViewSpecific);

                var curves = level.GetCurvesInView(DatumExtentType.ViewSpecific, view);
                if (curves.Count == 0) return;
                if (curves[0] is not Line line) return;

                var p0 = line.GetEndPoint(0);
                var p1 = line.GetEndPoint(1);
                var rightDir = view.RightDirection;

                XYZ rightPt = p1.DotProduct(rightDir) >= p0.DotProduct(rightDir) ? p1 : p0;

                double stub = 0.5 / 0.3048; // 500mm
                XYZ newStart = rightPt - rightDir.Multiply(stub);

                level.SetCurveInView(DatumExtentType.ViewSpecific, view, Line.CreateBound(newStart, rightPt));
            }
            catch { }
        }

        private HashSet<ElementId> GetWaterLevelIds()
        {
            var typeIds = new[] { "HWL", "LWL" }
                .Select(name => new FilteredElementCollector(_doc)
                    .OfClass(typeof(LevelType))
                    .Cast<LevelType>()
                    .FirstOrDefault(lt => lt.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Id)
                .Where(id => id != null)
                .ToHashSet();

            return new FilteredElementCollector(_doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .Where(l => typeIds.Contains(l.GetTypeId()))
                .Select(l => l.Id)
                .ToHashSet();
        }

        private void ApplyLevel(string typeName, double elevationFeet)
        {
            var levelType = new FilteredElementCollector(_doc)
                .OfClass(typeof(LevelType))
                .Cast<LevelType>()
                .FirstOrDefault(lt => lt.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));

            var existing = new FilteredElementCollector(_doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .FirstOrDefault(l => levelType != null
                    ? l.GetTypeId() == levelType.Id
                    : l.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                existing.Elevation = elevationFeet;
            }
            else
            {
                var level = Level.Create(_doc, elevationFeet);
                if (levelType != null)
                    level.ChangeTypeId(levelType.Id);
                else
                    level.Name = typeName;
            }
        }

        public (string hwl, string lwl) GetWaterLevels()
        {
            return (ReadLevelInMeters("HWL"), ReadLevelInMeters("LWL"));
        }

        private string ReadLevelInMeters(string typeName)
        {
            var levelType = new FilteredElementCollector(_doc)
                .OfClass(typeof(LevelType))
                .Cast<LevelType>()
                .FirstOrDefault(lt => lt.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));

            var level = new FilteredElementCollector(_doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .FirstOrDefault(l => levelType != null
                    ? l.GetTypeId() == levelType.Id
                    : l.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));

            if (level == null) return null;
            return (level.Elevation * 0.3048).ToString("F3", CultureInfo.InvariantCulture);
        }

        private static double? ParseMetersToFeet(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            var normalized = input.Replace(',', '.');
            if (!double.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var meters))
                return null;
            return meters / 0.3048;
        }
    }
}
