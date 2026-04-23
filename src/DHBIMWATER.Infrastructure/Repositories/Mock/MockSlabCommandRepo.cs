using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Infrastructure.Repositories.Mock
{
    public class MockSlabCommandRepo : ISlabCommandRepo
    {
        public int CreateSlab(SlabDefinition slabDef)
        {
            throw new NotImplementedException();
        }
    }
}
