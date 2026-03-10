using Autodesk.Revit.DB;
using DHBIMWATER.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Infrastructure.Repositories.Revit
{
    internal class RevitElementTypeQueryRepo : IElementTypeQueryRepo
    {
        public IEnumerable<string> GetBeamTypeNames()
        {
            throw new NotImplementedException();
            //new FilteredElementCollector();
        }

        public IEnumerable<string> GetColumnTypeNames()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetFoundationTypeNames()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetSlabTypeNames()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetWallTypeNames()
        {
            throw new NotImplementedException();
        }
    }
}
