using DHBIMWATER.Application.DTOs.Revit.Reservoir;
using Autodesk.Revit.DB;
using DHBIMWATER.Application.Interfaces;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DHBIMWATER.Infrastructure.Repositories.Revit
{
    internal class RevitWallCommandRepository : IWallCommandRepo
    {
        #region Fields
        private readonly Func<Document?> _doc;  // Revit Document에 접근하기 위한 람다식
        #endregion

        #region Properties
        #endregion

        #region Constructor
        public RevitWallCommandRepository(Func<Document?> doc)
        {
            _doc = doc;
        }
        #endregion

        #region Methods
        public void CreateWall()
        {
            Document? doc = _doc();
            
            if (doc == null)
            {
                TaskDialog.Show("Error", "Active document is not available.");
                return;
            }

            Curve curve = Line.CreateBound(new XYZ(0, 0, 0), new XYZ(10, 0, 0));
            Level? lv = new FilteredElementCollector(doc)
                            .OfClass(typeof(Level))
                            .Cast<Level>()
                            .FirstOrDefault(l => l.Name == "레벨 1");

            Wall.Create(doc, curve, lv.Id, true);

            TaskDialog.Show("RevitWallCommandRepo", $"CreateWall - Revit Implementation\n커브 길이: {curve.Length}");
        }
        #endregion

    }
}
