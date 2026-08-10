using Autodesk.Revit.DB;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Structures;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit.Modeling
{
    public class RevitSlabCommandRepo : ISlabCommandRepo
    {
        private readonly Func<Document?> _doc;
        private readonly IElementTypeCommandRepo _elementTypeCmdRepo;

        // TODO: 설정값에서 가져오도록 변경 — 굵은골재최대치수-압축강도-슬럼프
        private static readonly ConcreteSpec _concrete = new ConcreteSpec(25, 30, 150);

        public RevitSlabCommandRepo(Func<Document?> doc, IElementTypeCommandRepo elementTypeRepo)
        {
            _doc = doc;
            _elementTypeCmdRepo = elementTypeRepo;
        }

        public int CreateSlab(SlabDefinition slabDef, ConcreteSpec? concrete = null)
        {
            var doc = _doc();
            if (doc == null) return 0;

            var elementId = 0;
            List<XYZ> boundaryPoints = new List<XYZ>();
            CurveLoop curveLoop = new CurveLoop();
            IList<CurveLoop> curveLoopList = new List<CurveLoop>();

            foreach (Point2D pt in slabDef.Points)
            {
                boundaryPoints.Add(new XYZ(UC.MmToFt(pt.X), UC.MmToFt(pt.Y), 0));
            }

            for (int i = 0; i < boundaryPoints.Count; i++)
            {
                var startPt = boundaryPoints[i];
                var endPt = boundaryPoints[(i + 1) % boundaryPoints.Count];
                Line line = Line.CreateBound(startPt, endPt);
                curveLoop.Append(line);
            }
            curveLoopList.Add(curveLoop);

            var subPoints = new List<XYZ>();
            CurveLoop subCurveLoop = new CurveLoop();

            if (slabDef.SubPoints != null)
            {
                foreach (Point2D pt in slabDef.SubPoints)
                {
                    subPoints.Add(new XYZ(UC.MmToFt(pt.X), UC.MmToFt(pt.Y), 0));
                }

                for (int i = 0; i < subPoints.Count; i++)
                {
                    var startPt = subPoints[i];
                    var endPt = subPoints[(i + 1) % subPoints.Count];
                    Line line = Line.CreateBound(startPt, endPt);
                    subCurveLoop.Append(line);
                }

                if (subPoints.Count > 0)
                    curveLoopList.Add(subCurveLoop);
            }

            var floorSpec = new FloorTypeSpec(slabDef.Thickness, $"일반 - {slabDef.Thickness}mm", concrete ?? _concrete);
            var floorTypeId = new ElementId((long)_elementTypeCmdRepo.FindOrCreateSlabType(floorSpec));

            var level = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .FirstOrDefault(e => e.Name.Equals(slabDef.LevelName));
            if (level == null)
            {
                throw new Exception($"Level '{slabDef.LevelName}' not found.");
            }

            var floor = Floor.Create(doc, curveLoopList, floorTypeId, level.Id);
            // Z값 조정
            floor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM)?.Set(UC.MmToFt(slabDef.ElevationZ) - level.Elevation); ;
            //floor.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).Set(slabDef.ElementCode);
            floor.LookupParameter("DH_Addin")?.Set("DHBIMWATER");
            floor.LookupParameter("DH_Category")?.Set(slabDef.Category);
            floor.LookupParameter("DH_ElementCode")?.Set(slabDef.ElementCode);
            floor.LookupParameter("DH_Part")?.Set(slabDef.Part);
            floor.LookupParameter("DH_Zone")?.Set(slabDef.Zone);

            return elementId;
        }
    }
}
