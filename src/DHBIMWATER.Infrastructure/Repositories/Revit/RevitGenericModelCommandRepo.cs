using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Structures;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit
{
    internal class RevitGenericModelCommandRepo : IGenericModelCommandRepo
    {
        private readonly Func<Document?> _doc;
        private readonly IDialogService _dialog;

        public RevitGenericModelCommandRepo(Func<Document?> doc, IDialogService dialog)
        {
            _doc = doc;
            _dialog = dialog;
        }

        public int PlaceInstance(GenericModelPlacementDefinition def)
        {
            Document? doc = _doc();
            if (doc == null) return 0;

            FamilySymbol? symbol = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_GenericModel)
                .Cast<FamilySymbol>()
                .FirstOrDefault(s => s.Name.Contains(def.SymbolName));

            if (symbol == null)
            {
                _dialog.Warn("Error", $"패밀리 타입을 찾을 수 없습니다: {def.SymbolName}");
                return 0;
            }

            Level? level = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .FirstOrDefault(l => l.Name == def.LevelName);

            if (level == null)
            {
                _dialog.Warn("Error", $"레벨을 찾을 수 없습니다: {def.LevelName}");
                return 0;
            }

            if (!symbol.IsActive) symbol.Activate();

            var origin = new XYZ(UC.MmToFt(def.Origin.X),
                                 UC.MmToFt(def.Origin.Y),
                                 UC.MmToFt(def.Origin.Z));

            FamilyInstance instance = doc.Create.NewFamilyInstance(
                origin, symbol, level, StructuralType.Footing);

            if (instance == null) TaskDialog.Show("Error", "패밀리인스턴스가 배치되지않았습니다");

            if (def.Rotation != 0.0)
            {
                var axis = Line.CreateBound(origin, origin + XYZ.BasisZ);
                ElementTransformUtils.RotateElement(doc, instance.Id, axis, UC.DegToRad(def.Rotation)); // CCW
            }

            instance.LookupParameter("DH_Addin")?.Set("DHBIMWATER");
            instance.LookupParameter("DH_ElementCode")?.Set(def.ElementCode);
            instance.LookupParameter("DH_Part")?.Set(def.Part);
            instance.LookupParameter("DH_Zone")?.Set(def.Zone);

            foreach (var (name, value) in def.Parameters)
            {
                var param = instance.LookupParameter(name);
                if (param == null) continue;

                switch (value)
                {
                    case string s: param.Set(s); break;
                    case double d: param.Set(UC.MmToFt(d)); break;
                    case int i:    param.Set(i); break;
                    default: break;
                }
            }
            return (int)instance.Id.Value;
        }
    }
}
