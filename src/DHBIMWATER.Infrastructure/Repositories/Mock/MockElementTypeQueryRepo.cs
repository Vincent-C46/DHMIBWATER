using DHBIMWATER.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Infrastructure.Repositories.Mock
{
    internal class MockElementTypeQueryRepo : IElementTypeQueryRepo
    {
        public IEnumerable<string> GetBeamTypeNames()
        {
            throw new NotImplementedException();
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
