using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Structures;
using System.Diagnostics;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit
{
    public class RevitDirectShapeCommandRepo : IDirectShapeCommandRepo
    {
        private readonly Func<Document?> _doc;

        public RevitDirectShapeCommandRepo(Func<Document?> doc)
        {
            _doc = doc;
        }
        public int CreateDirectShape(SolidExtrusionDefinition solidExtrusionDef)
        {
            var doc = _doc();
            if (doc == null) return 0;

            var geometry = BuildSolid(solidExtrusionDef);
            var ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_Floors));
            ds.SetShape(new GeometryObject[] { geometry });
            ds.Name = solidExtrusionDef.ElementCode;
            return (int)ds.Id.Value;
        }

        public IReadOnlyList<int> CreateDirectShapes(IReadOnlyList<SolidExtrusionDefinition> solidExtrusionDefs)
        {
            var doc = _doc();
            if (doc == null) return new List<int>() { 0 };

            var ids = new List<int>();

            foreach (var group in solidExtrusionDefs.GroupBy(d => d.ElementCode))
            {
                //TaskDialog.Show("info", $"=== 그룹: {group.Key} ({group.Count()}개) ===");
                var solids = group.Select(def => BuildSolid(def)).ToList();
                if (solids.Count == 0) continue;

                Solid merged = solids[0]; 
                foreach (var solid in solids.Skip(1))
                {
                    try
                    {
                        merged = BooleanOperationsUtils.ExecuteBooleanOperation(
                            merged, solid, BooleanOperationsType.Union);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Union 실패: {ex.Message}");
                        // 실패한 솔리드는 별도 DirectShape로 생성
                        var failedDs = DirectShape.CreateElement(
                            doc, new ElementId(BuiltInCategory.OST_Floors));
                        failedDs.SetShape(new GeometryObject[] { solid });
                        failedDs.Name = $"{group.Key}_failed";
                    }
                }

                var ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_Floors));
                ds.SetShape(new GeometryObject[] { merged});
                ds.Name = group.Key;

                ids.Add((int)ds.Id.Value);
            }
            return ids;
        }

        private Solid BuildSolid(SolidExtrusionDefinition def)
        {
            CurveLoop curveLoop = new CurveLoop();
            var ptNum = def.Profile.Count;
            for (int i = 0; i < ptNum; i++)
            {
                var start = def.Profile[i];
                var end = def.Profile[(i + 1) % ptNum];
                var line = Line.CreateBound(new XYZ(UC.MmToFt(start.X), UC.MmToFt(start.Y), UC.MmToFt(start.Z)),
                    new XYZ(UC.MmToFt(end.X), UC.MmToFt(end.Y), UC.MmToFt(end.Z)));
                curveLoop.Append(line);
            }
            // 재료 지정 필요
            SolidOptions solidOptions = new SolidOptions(ElementId.InvalidElementId, ElementId.InvalidElementId);
            Solid solid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { curveLoop },
                new XYZ(def.Normal.X, def.Normal.Y, def.Normal.Z),
                UC.MmToFt(def.Distance),
                solidOptions);

            return solid;
        }
    }
}
