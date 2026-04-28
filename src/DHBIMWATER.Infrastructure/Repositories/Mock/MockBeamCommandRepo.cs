using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Structures;


namespace DHBIMWATER.Infrastructure.Repositories.Mock
{
    public class MockBeamCommandRepo : IBeamCommandRepo
    {
        public int CreateBeam(BeamDefinition beamDef)
        {
            throw new NotImplementedException();
        }
    }
}
