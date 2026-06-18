using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Structures;
using System.Diagnostics;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Modeling
{
    public class RevitDirectShapeCommandRepo : IDirectShapeCommandRepo
    {
        private readonly Func<Document?> _doc;

        // TODO: 설정값에서 가져오도록 변경 — 굵은골재최대치수-압축강도-슬럼프
        private static readonly ConcreteSpec _concrete = new ConcreteSpec(25, 21, 120);

        public RevitDirectShapeCommandRepo(Func<Document?> doc)
        {
            _doc = doc;
        }

        private ElementId FindOrCreateConcreteMaterial(Document doc)
        {
            var allMaterials = new FilteredElementCollector(doc)
                .OfClass(typeof(Material))
                .Cast<Material>()
                .ToList();

            var existing = allMaterials.FirstOrDefault(m =>
                m.Name.Equals(_concrete.MaterialName, StringComparison.OrdinalIgnoreCase));
            if (existing != null) return existing.Id;

            var baseMaterial = allMaterials.FirstOrDefault(m =>
                m.Name.IndexOf("concrete", StringComparison.OrdinalIgnoreCase) >= 0 ||
                m.Name.Contains("콘크리트"));

            if (baseMaterial == null)
                return ElementId.InvalidElementId;

            var newMat = baseMaterial.Duplicate(_concrete.MaterialName) as Material;
            var strength = UnitUtils.ConvertToInternalUnits(_concrete.CompressiveStrength, UnitTypeId.Megapascals);
            newMat.get_Parameter(BuiltInParameter.PHY_MATERIAL_PARAM_CONCRETE_COMPRESSION)?.Set(strength);
            return newMat.Id;
        }
        public int CreateDirectShape(SolidExtrusionDefinition solidExtrusionDef)
        {
            var doc = _doc();
            if (doc == null) return 0;

            var materialId = FindOrCreateConcreteMaterial(doc);
            var geometry = BuildSolid(solidExtrusionDef, materialId);
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
            var materialId = FindOrCreateConcreteMaterial(doc);

            foreach (var group in solidExtrusionDefs.GroupBy(d => d.ElementCode))
            {
                //TaskDialog.Show("info", $"=== 그룹: {group.Key} ({group.Count()}개) ===");
                var solids = group.Select(def => BuildSolid(def, materialId)).ToList();
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
                ds.LookupParameter("DH_Category")?.Set("DirectShape");

                ids.Add((int)ds.Id.Value);
            }
            return ids;
        }

        private Solid BuildSolid(SolidExtrusionDefinition def, ElementId materialId)
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
            SolidOptions solidOptions = new SolidOptions(materialId, ElementId.InvalidElementId);
            Solid solid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { curveLoop },
                new XYZ(def.Normal.X, def.Normal.Y, def.Normal.Z),
                UC.MmToFt(def.Distance),
                solidOptions);

            return solid;
        }
    }
}
