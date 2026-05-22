using Accessibility;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using DHBIMWATER.Application.DTOs.Revit.Reservoir;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Structures;
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
    internal class RevitWallCommandRepo : IWallCommandRepo
    {
        #region Fields
        private readonly Func<Document?> _doc;  // Revit Document에 접근하기 위한 람다식
        private readonly IDialogService _dialog;
        private readonly IElementTypeCommandRepo _elementTypeCmdRepo;
        #endregion

        #region Properties
        #endregion

        #region Constructor
        public RevitWallCommandRepo(Func<Document?> doc, IDialogService dialog, IElementTypeCommandRepo elementTypeCmdRepo)
        {
            _doc = doc;
            _dialog = dialog;
            _elementTypeCmdRepo = elementTypeCmdRepo;
        }
        #endregion

        #region Methods
        public int CreateLinearWall(LinearWallDefinition linearWallDefinition)
        {
            Document? doc = _doc();

            if (doc == null)
            {
                _dialog.Warn("Error", "Active document is not available.");
                return 0;
            }
            var elementId = 0;

            Level? wallLevel = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .FirstOrDefault(l => l.Name == linearWallDefinition.LevelName);

            if (wallLevel == null)
            {
                _dialog.Warn("Error", "Wall 하단 레벨이 설정되지않았습니다.");
                return 0;
            }
            XYZ startPt = new XYZ(UC.MmToFt(linearWallDefinition.StartPoint.X),
                                  UC.MmToFt(linearWallDefinition.StartPoint.Y),
                                  UC.MmToFt(linearWallDefinition.StartPoint.Z));
            XYZ endPt = new XYZ(UC.MmToFt(linearWallDefinition.EndPoint.X),
                                UC.MmToFt(linearWallDefinition.EndPoint.Y),
                                UC.MmToFt(linearWallDefinition.EndPoint.Z));
            Curve wallCurve = Line.CreateBound(startPt, endPt);

            var wallSpec = new WallTypeSpec(linearWallDefinition.Thickness, $"일반 - {linearWallDefinition.Thickness}mm");
            var wallTypeId = new ElementId((long)_elementTypeCmdRepo.FindOrCreateWallType(wallSpec));

            //if (linearWallDefinition.Height <= 0)
            //{
            //    _dialog.Warn("Error", $"벽체 높이가 0이하 입니다.\nElementCode{linearWallDefinition.ElementCode}\nHeight: {linearWallDefinition.Height}");
            //}

            Wall wall;

            try
            {
                wall = Wall.Create(doc, wallCurve, wallTypeId, wallLevel.Id,
                        UC.MmToFt(linearWallDefinition.Height),
                        UC.MmToFt(linearWallDefinition.BaseOffset),
                        linearWallDefinition.IsFlipped, true);
            }
            catch (Exception ex)
            {
                _dialog.Warn("Error", $"벽체 생성 실패\nElementCode: {linearWallDefinition.ElementCode}\nException: {ex.Message}");
                return 0;
            }


            WallUtils.DisallowWallJoinAtEnd(wall, 0);
            WallUtils.DisallowWallJoinAtEnd(wall, 1);

            //wall.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).Set(linearWallDefinition.ElementCode);
            wall.LookupParameter("DH_Addin")?.Set("DHBIMWATER");
            wall.LookupParameter("DH_ElementCode")?.Set(linearWallDefinition.ElementCode);
            wall.LookupParameter("DH_Part")?.Set(linearWallDefinition.Part);
            wall.LookupParameter("DH_Zone")?.Set(linearWallDefinition.Zone);
            wall.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).Set("");

            //_dialog.Info("RevitWallCommandRepo", $"CreateWall - Revit Implementation\n 벽체 높이: {linearWallDefinition.Height}mm");

            return (int)wall.Id.Value;
        }
        public int CreateProfileWall(ProfileWallDefinition profileWallDefinition)
        {
            Document? doc = _doc();

            if (profileWallDefinition.Points.Count < 3)
            {
                _dialog.Warn("Error", "벽체 프로파일 점 개수가 3개 미만입니다");
                return 0;
            }

            var wallSpec = new WallTypeSpec(profileWallDefinition.Thickness, $"일반 - {profileWallDefinition.Thickness}mm");

            var wallTypeIntId = _elementTypeCmdRepo.FindOrCreateWallType(wallSpec);
            if (wallTypeIntId == 0) { _dialog.Warn("Error", "WallType 생성 실패"); return 0; }
            var wallTypeId = new ElementId((long)wallTypeIntId);

            Level? wallLevel = new FilteredElementCollector(doc)
                                .OfClass(typeof(Level))
                                .Cast<Level>()
                                .FirstOrDefault(l => l.Name == profileWallDefinition.LevelName);

            if (wallLevel == null)
            {
                _dialog.Warn("Error", "레벨 지정 실패. (프로파일 벽체)");
                return 0;
            }

            var profiles = new List<Curve>();
            int numPoints = profileWallDefinition.Points.Count;

            for (int i = 0; i < numPoints; i++)
            {
                Point3D startPt = profileWallDefinition.Points[i];
                Point3D endPt = profileWallDefinition.Points[(i + 1) % numPoints];

                XYZ startXYZ = new XYZ(UC.MmToFt(startPt.X), UC.MmToFt(startPt.Y), UC.MmToFt(startPt.Z));
                XYZ endXYZ = new XYZ(UC.MmToFt(endPt.X), UC.MmToFt(endPt.Y), UC.MmToFt(endPt.Z));

                Curve line = Line.CreateBound(startXYZ, endXYZ);
                profiles.Add(line);
            }

            Wall profileWall;
            try
            {
                profileWall = Wall.Create(doc, profiles, wallTypeId, wallLevel.Id, true);
            }
            catch( Exception ex)
            {
                _dialog.Warn("Error", $"프로파일 벽체 생성 실패\nElementCode: {profileWallDefinition.ElementCode}\nException: {ex.Message}");
                return 0;
            }

            //profileWall.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).Set(profileWallDefinition.ElementCode);
            profileWall.LookupParameter("DH_ElementCode")?.Set(profileWallDefinition.ElementCode);
            profileWall.LookupParameter("DH_Addin")?.Set("DHBIMWATER");
            profileWall.LookupParameter("DH_Part")?.Set(profileWallDefinition.Part);
            profileWall.LookupParameter("DH_Zone")?.Set(profileWallDefinition.Zone);

            if (profileWallDefinition.IsFlipped) profileWall.Flip();

            WallUtils.DisallowWallJoinAtEnd(profileWall, 0);
            WallUtils.DisallowWallJoinAtEnd(profileWall, 1);

            return (int)(profileWall.Id.Value);
        }
        #endregion
    }
}