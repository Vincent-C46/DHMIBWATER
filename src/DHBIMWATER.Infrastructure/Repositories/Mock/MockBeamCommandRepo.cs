using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Structures;
using DHBIMWATER.Infrastructure.Services.Mock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DHBIMWATER.Infrastructure.Repositories.Mock
{
    public class MockBeamCommandRepo : IBeamCommandRepo
    {
        public int CreateBeam(BeamDefinition beamDef)
        {
            var mockDialogService = new MockDialogService();
            mockDialogService.Info("Beam Creation", $"{beamDef.ElementCode} 작성완료");
            return 0;
        }
    }
}
