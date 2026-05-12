using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;

namespace DHBIMWATER.Infrastructure.Services.Revit.Sheets
{
    public class DimensionSelectionService
    {
        private readonly UIDocument _uidoc;
        public DimensionSelectionService(UIDocument uidoc) { _uidoc = uidoc; }

        public IList<string> PickTargetIds()
        {
            var refs = _uidoc.Selection.PickObjects(ObjectType.Element, "치수선 생성할 객체 선택");
            return refs.Select(r => r.ElementId.Value.ToString()).ToList();
        }
    }

}