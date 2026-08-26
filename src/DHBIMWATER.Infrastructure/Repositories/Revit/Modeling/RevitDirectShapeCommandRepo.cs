using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Structures;
using System.Diagnostics;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Modeling
{
    public class RevitDirectShapeCommandRepo : IDirectShapeCommandRepo
    {
        private readonly Func<Document?> _doc;
        private readonly IElementTypeCommandRepo _elementTypeCmdRepo;

        // TODO: 설정값에서 가져오도록 변경 — 굵은골재최대치수-압축강도-슬럼프
        private static readonly ConcreteSpec _concrete = new ConcreteSpec(25, 21, 120);

        public RevitDirectShapeCommandRepo(Func<Document?> doc, IElementTypeCommandRepo elementTypeCmdRepo)
        {
            _doc = doc;
            _elementTypeCmdRepo = elementTypeCmdRepo;
        }

        public int CreateDirectShape(SolidExtrusionDefinition solidExtrusionDef)
        {
            var doc = _doc();
            if (doc == null) return 0;

            var materialId = new ElementId(_elementTypeCmdRepo.FindOrCreateConcreteMaterial(_concrete));
            var geometry = BuildExtrusion(solidExtrusionDef, materialId);
            var ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_Floors));
            ds.SetShape(new GeometryObject[] { geometry });
            ds.Name = solidExtrusionDef.ElementCode;
            return (int)ds.Id.Value;
        }

        public IReadOnlyList<int> CreateDirectShapes(IReadOnlyList<SolidExtrusionDefinition> solidExtrusionDefs, ConcreteSpec? concrete = null)
        {
            var doc = _doc();
            if (doc == null) return new List<int>() { 0 };

            var ids = new List<int>();
            var materialId = new ElementId(_elementTypeCmdRepo.FindOrCreateConcreteMaterial(concrete ?? _concrete));

            foreach (var group in solidExtrusionDefs.GroupBy(d => d.ElementCode))
            {
                //TaskDialog.Show("info", $"=== 그룹: {group.Key} ({group.Count()}개) ===");
                var solids = group.Select(def => BuildExtrusion(def, materialId)).ToList();
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
                        //Debug.WriteLine($"Union 실패: {ex.Message}");
                        var failedDs = DirectShape.CreateElement(
                            doc, new ElementId(BuiltInCategory.OST_Floors));
                        failedDs.SetShape(new GeometryObject[] { solid });
                        failedDs.Name = $"{group.Key}_failed";
                    }
                }
                var ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_Floors));
                ds.SetShape(new GeometryObject[] { merged});
                ds.Name = group.Key;
                //ds.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).Set(group.Key);
                ds.LookupParameter("DH_ElementCode")?.Set(group.Key);
                ds.LookupParameter("DH_Addin")?.Set("DHBIMWATER");
                ds.LookupParameter("DH_Part")?.Set(group.ElementAt(0).Part);
                ds.LookupParameter("DH_Category")?.Set(group.ElementAt(0).Category);

                ids.Add((int)ds.Id.Value);
            }
            return ids;
        }

        private Solid BuildExtrusion(SolidExtrusionDefinition def, ElementId materialId)
        {
            Solid solid = CreateExtrusion(def.Profile, def.Normal, def.Distance, materialId);

            foreach (var voidDef in def.Voids)
            {
                var voidSolid = CreateExtrusion(voidDef.Profile, voidDef.Normal, voidDef.Distance, materialId);
                solid = BooleanOperationsUtils.ExecuteBooleanOperation(solid, voidSolid, BooleanOperationsType.Difference);
            }

            return solid;
        }

        private Solid CreateExtrusion(
            IReadOnlyList<Point3D> profile,
            Vector3D normal,
            double distance,
            ElementId materialId)
        {
            var curveLoop = new CurveLoop();

            for (int i = 0; i < profile.Count; i++)
            {
                var start = profile[i];
                var end = profile[(i + 1) % profile.Count];

                var line = Line.CreateBound(
                    new XYZ(UC.MmToFt(start.X), UC.MmToFt(start.Y), UC.MmToFt(start.Z)),
                    new XYZ(UC.MmToFt(end.X), UC.MmToFt(end.Y), UC.MmToFt(end.Z)));

                curveLoop.Append(line);
            }

            var solidOptions = new SolidOptions(materialId, ElementId.InvalidElementId);

            return GeometryCreationUtilities.CreateExtrusionGeometry(
                new List<CurveLoop> { curveLoop },
                new XYZ(normal.X, normal.Y, normal.Z),
                UC.MmToFt(distance),
                solidOptions);
        }
    }
}
