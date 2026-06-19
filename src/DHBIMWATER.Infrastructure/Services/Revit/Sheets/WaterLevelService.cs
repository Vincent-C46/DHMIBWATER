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

                var p0       = line.GetEndPoint(0);
                var p1       = line.GetEndPoint(1);
                var rightDir = view.RightDirection.Normalize();

                // 크롭 박스 중앙 R 위치 계산 → 머리기호를 뷰 중앙에 배치
                var    cb         = view.CropBox;
                var    t          = cb.Transform;
                double cropMinR   = t.OfPoint(new XYZ(cb.Min.X, 0, 0)).DotProduct(rightDir);
                double cropMaxR   = t.OfPoint(new XYZ(cb.Max.X, 0, 0)).DotProduct(rightDir);
                double centerR    = (cropMinR + cropMaxR) * 0.5;

                // 기존 라인 위의 centerR 위치 보간
                double r0     = p0.DotProduct(rightDir);
                double r1     = p1.DotProduct(rightDir);
                double tParam = Math.Abs(r1 - r0) > 1e-6
                    ? Math.Clamp((centerR - r0) / (r1 - r0), 0.05, 0.95)
                    : 0.5;
                XYZ headPt  = p0 + (p1 - p0).Multiply(tParam);

                double stub   = 0.001 / 0.3048; // 1mm — 머리기호만
                XYZ startPt   = headPt - rightDir.Multiply(stub);

                level.SetCurveInView(DatumExtentType.ViewSpecific, view, Line.CreateBound(startPt, headPt));
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
