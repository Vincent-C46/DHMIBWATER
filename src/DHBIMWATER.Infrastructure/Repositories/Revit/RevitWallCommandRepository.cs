using Accessibility;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.DTOs.Revit.Reservoir;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Infrastructure.Services.Revit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using GC = DHBIMWATER.Infrastructure.Converters.RevitGeometryConverter;
using UC = DHBIMWATER.Infrastructure.Converters.RevitUnitConverter;

namespace DHBIMWATER.Infrastructure.Repositories.Revit
{
    internal class RevitWallCommandRepository : IWallCommandRepo
    {
        #region Fields
        private readonly Func<Document?> _doc;  // Revit Document에 접근하기 위한 람다식
        private readonly IDialogService _dialog;
        #endregion

        #region Properties
        #endregion

        #region Constructor
        public RevitWallCommandRepository(Func<Document?> doc, IDialogService dialog)
        {
            _doc = doc;
            _dialog = dialog;
        }
        #endregion

        #region Methods
        public int CreateWall(double len, double n)
        {
            Document? doc = _doc();

            if (doc == null)
            {
                _dialog.Warn("Error", "Active document is not available.");
                return 0;
            }

            Curve curve = Line.CreateBound(new XYZ(0, 0, 0), new XYZ(len, 0, 0));
            Curve curve2 = Line.CreateBound(new XYZ(len, 0, 0), new XYZ(len * n, 0, 0));

            Level? lv = new FilteredElementCollector(doc)
                            .OfClass(typeof(Level))
                            .Cast<Level>()
                            .FirstOrDefault(l => l.Name == "레벨 1");

            Wall.Create(doc, curve, lv.Id, true);
            Wall.Create(doc, curve2, lv.Id, true);

            _dialog.Info("RevitWallCommandRepo", $"CreateWall - Revit Implementation\n커브 길이: {curve.Length}");
            return 1;
        }

        public int CreateProfileWall(IList<Point3D> profilePoints_mm, string wallTypeName, string levelName)
        {
            Document? doc = _doc();

            if (profilePoints_mm.Count < 3) return 0;
            var profiles = new List<Curve>();
            int numPoints = profilePoints_mm.Count;

            ElementId wallTypeId = new FilteredElementCollector(doc)
                                        .OfCategory(BuiltInCategory.OST_Walls)
                                        .WhereElementIsElementType()
                                        .Cast<WallType>()
                                        .FirstOrDefault(wt => wt.Name == wallTypeName)?.Id ?? throw new Exception($"Wall type '{wallTypeName}' not found.");
            ElementId levelId = new FilteredElementCollector(doc)
                                      .OfCategory(BuiltInCategory.OST_Levels)
                                      .WhereElementIsNotElementType()
                                      .Cast<Level>()
                                      .FirstOrDefault(l => l.Name == levelName)?.Id ?? throw new Exception($"Level '{levelName}' not found.");
            if (wallTypeId == null || levelId == null)
            {
                _dialog.Warn("Error", "Specified wall type or level not found.");
                return 0;
            }

            // 벡터 a, 벡터 b
            XYZ p0 = new XYZ(UC.MmToFt(profilePoints_mm[0].X), UC.MmToFt(profilePoints_mm[0].Y), UC.MmToFt(profilePoints_mm[0].Z));
            XYZ p1 = new XYZ(UC.MmToFt(profilePoints_mm[1].X), UC.MmToFt(profilePoints_mm[1].Y), UC.MmToFt(profilePoints_mm[1].Z));
            XYZ p2 = new XYZ(UC.MmToFt(profilePoints_mm[2].X), UC.MmToFt(profilePoints_mm[2].Y), UC.MmToFt(profilePoints_mm[2].Z));

            XYZ a = p1 - p0;
            XYZ b = p2 - p0;

            XYZ normal = a.CrossProduct(b).Normalize();

            TaskDialog.Show("Normal", $"X:{normal.X:F2} Y:{normal.Y:F2} Z:{normal.Z:F2}");

            for (int i = 0; i < profilePoints_mm.Count; i++)
            {
                Point3D startPt = profilePoints_mm[i];
                Point3D endPt = profilePoints_mm[(i + 1) % numPoints];

                XYZ startXYZ = new XYZ(UC.MmToFt(startPt.X), UC.MmToFt(startPt.Y), UC.MmToFt(startPt.Z));
                XYZ endXYZ = new XYZ(UC.MmToFt(endPt.X), UC.MmToFt(endPt.Y), UC.MmToFt(endPt.Z));

                Curve line = Line.CreateBound(startXYZ, endXYZ);
                profiles.Add(line);
            }

            var wall = Wall.Create(doc, profiles, wallTypeId, levelId, true);
            return (int)(wall.Id.Value);
        }

        public void CreateWallType()
        {

        }
        #endregion

    }
}
