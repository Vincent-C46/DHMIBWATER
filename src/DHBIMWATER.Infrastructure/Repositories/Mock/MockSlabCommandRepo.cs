using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Structures;
using DHBIMWATER.Infrastructure.Services.Mock;
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
            var mockDialogService = new MockDialogService();
            mockDialogService.Info("Slab Creation", $"슬래브 작성 완료");
            return 0;
        }
    }
}
