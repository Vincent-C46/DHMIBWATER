using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Structures;
using System.Windows.Controls;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit
{
    public class RevitOpeningCommandRepo : IOpeningCommandRepo
    {
        private readonly Func<Document?> _doc;
        private readonly IDialogService _dialogService;

        public RevitOpeningCommandRepo(Func<Document?> doc, IDialogService dialogService)
        {
            _doc = doc;
            _dialogService = dialogService;
        }

        public void CreateSlabOpening(RectangularSlabOpeningDefinition openingDef)
        {
            var doc = _doc();
            if (doc == null) return;

            var elementId = 0;

            // 슬래브 사각형 오프닝
            FamilySymbol symbol = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_GenericModel)
                .WhereElementIsElementType()
                .OfType<FamilySymbol>()
                .FirstOrDefault(fs => fs.Name.Contains("바닥 개구부_사각형"));

            if (symbol == null)
            {
                _dialogService.Info("Error", "Required family symbol '바닥 개구부_사각형' not found.");
                return;
            }

            XYZ position = new XYZ(UC.MmToFt(openingDef.Position.X), UC.MmToFt(openingDef.Position.Y), 0);

            var host = new FilteredElementCollector(doc)
                    .OfClass(typeof(Floor))
                    .WhereElementIsNotElementType()
                    .Cast<Floor>()
                    .FirstOrDefault(f => f.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsValueString() == openingDef.HostElementCode);

            var hostLevel = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Levels)
                .WhereElementIsNotElementType()
                .Cast<Level>()
                .FirstOrDefault(l => l.Name == openingDef.LevelName);
            var opening = doc.Create.NewFamilyInstance(position, symbol, host, hostLevel, StructuralType.NonStructural);
            opening.LookupParameter("W").Set(UC.MmToFt(openingDef.Width));
            opening.LookupParameter("L").Set(UC.MmToFt(openingDef.Length));
            //opening.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).Set(UC.MmToFt(2320));   

            return;
        }

        public void CreateSlabOpening(CircularSlabOpeningDefinition openingDef)
        {
            throw new NotImplementedException();
        }

        public void CreateWallOpening(RectangularWallOpeningDefinition openingDef)
        {
            var doc = _doc();
            if (doc == null) return;

            // 슬래브 사각형 오프닝
            FamilySymbol symbol = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_GenericModel)
                .WhereElementIsElementType()
                .OfType<FamilySymbol>()
                .FirstOrDefault(fs => fs.Name.Contains("벽 개구부_사각형"));

            if (symbol == null)
            {
                _dialogService.Info("Error", "Required family symbol '벽 개구부_사각형' not found.");
                return;
            }

            XYZ position = new XYZ(UC.MmToFt(openingDef.Position.X), UC.MmToFt(openingDef.Position.Y), UC.MmToFt(openingDef.Position.Z));

            var hosts = new FilteredElementCollector(doc)
                    .OfClass(typeof(Wall))
                    .WhereElementIsNotElementType()
                    .Cast<Wall>()
                    .Where(f => f.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsValueString() == openingDef.HostElementCode)
                    .ToList();

            var hostLevel = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Levels)
                .WhereElementIsNotElementType()
                .Cast<Level>()
                .FirstOrDefault(l => l.Name == openingDef.LevelName);

            foreach (var host in hosts)
            {
                var opening = doc.Create.NewFamilyInstance(position, symbol, host, hostLevel, StructuralType.NonStructural);
                opening.LookupParameter("W").Set(UC.MmToFt(openingDef.Width));
                opening.LookupParameter("H").Set(UC.MmToFt(openingDef.Height));
                opening.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).Set(UC.MmToFt(openingDef.OffsetZ));
            }

            return;
        }

        public void CreateWallOpening(CircularWallOpeningDefinition openingDef)
        {
            var doc = _doc();
            if (doc == null) return;

            // 슬래브 사각형 오프닝
            FamilySymbol symbol = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_GenericModel)
                .WhereElementIsElementType()
                .OfType<FamilySymbol>()
                .FirstOrDefault(fs => fs.Name.Contains("벽 개구부_원형"));

            if (symbol == null)
            {
                _dialogService.Info("Error", "Required family symbol '벽 개구부_원형' not found.");
                return;
            }

            XYZ position = new XYZ(UC.MmToFt(openingDef.Position.X), UC.MmToFt(openingDef.Position.Y), UC.MmToFt(openingDef.Position.Z));

            var host = new FilteredElementCollector(doc)
                    .OfClass(typeof(Wall))
                    .WhereElementIsNotElementType()
                    .Cast<Wall>()
                    .FirstOrDefault(f => f.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsValueString() == openingDef.HostElementCode);

            var hostLevel = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Levels)
                .WhereElementIsNotElementType()
                .Cast<Level>()
                .FirstOrDefault(l => l.Name == openingDef.LevelName);

            var opening = doc.Create.NewFamilyInstance(position, symbol, host, hostLevel, StructuralType.NonStructural);
            opening.LookupParameter("D").Set(UC.MmToFt(openingDef.Diameter));
            opening.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).Set(UC.MmToFt(openingDef.OffsetZ));

            return;
        }
    }
}
