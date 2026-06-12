using DHBIMWATER.Application.Interfaces;
using DHBIMWATER.Core.Structures;
using DHBIMWATER.Infrastructure.Services.Mock;

namespace DHBIMWATER.Infrastructure.Repositories.Mock
{
    internal class MockGenericModelCommandRepo : IGenericModelCommandRepo
    {
        public int PlaceInstance(GenericModelPlacementDefinition def)
        {
            var mockDialogService = new MockDialogService();
            mockDialogService.Info("GenericModel Placement", $"{def.SymbolName} ({def.ElementCode}) 배치완료");
            return 0;
        }
    }
}
