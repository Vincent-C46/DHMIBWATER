using DHBIMWATER.Application.Interfaces;
using Autodesk.Revit.DB;
using DHBIMWATER.Core.Structures;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DHBIMWATER.Core.Geometry;

namespace DHBIMWATER.Infrastructure.Repositories.Revit
{
    public class RevitSlabCommandRepo : ISlabCommandRepo
    {
        private readonly Func<Document?> _doc;

        public RevitSlabCommandRepo(Func<Document?> doc)
        {
            _doc = doc;
        }

        public int CreateSlab(SlabDefinition slabDef)
        {
            var doc = _doc();
            if (doc == null) return 0;

            var elementId = 0;
            List<XYZ> boundaryPoints = new List<XYZ>();
            CurveLoop curveLoop = new CurveLoop();

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
            IList<CurveLoop> curveLoopList = new List<CurveLoop> { curveLoop };

            var floorTypeId = new FilteredElementCollector(doc)
                .OfClass(typeof(FloorType))
                .FirstOrDefault(e => e.Name.Equals("일반 300mm"))?.Id ?? ElementId.InvalidElementId;
            var levelId = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .FirstOrDefault(e => e.Name.Equals(slabDef.LevelName))?.Id ?? ElementId.InvalidElementId;

            Floor.Create(doc, curveLoopList, floorTypeId, levelId);

            return elementId;
        }
    }
}
