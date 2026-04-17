using Autodesk.Revit.DB;
using DHBIMWATER.Application.DTOs.Revit.Reservoir;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Infrastructure.Services.Revit;
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
        public void CreateWall(double len, double n)
        {
            Document? doc = _doc();
            
            if (doc == null)
            {
                _dialog.Warn("Error", "Active document is not available.");
                return;
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
        }
        #endregion

    }
}
