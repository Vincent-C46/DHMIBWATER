using DHBIMWATER.Application.DTOs.GuildeLine;
using DHBIMWATER.Application.Interfaces;

namespace DHBIMWATER.Infrastructure.Services.Mock
{
    public class MockGuideLineService : IGuideLineService
    {
        // 여기는 Mock 구현부로, 실제 Revit API 호출 없이 테스트용 데이터를 반환
        public List<RevitFamilyDto> GetFamilies()
        {
            List<RevitFamilyDto> comboItems = new List<RevitFamilyDto>
            {
                new RevitFamilyDto { FamilyName = "MockFamily - 1" },
                new RevitFamilyDto { FamilyName = "MockFamily - 2" },
                new RevitFamilyDto { FamilyName = "MockFamily - 3" },
                new RevitFamilyDto { FamilyName = "MockFamily - 4" },
                new RevitFamilyDto { FamilyName = "MockFamily - 5" },
                new RevitFamilyDto { FamilyName = "MockFamily - 6" },
                new RevitFamilyDto { FamilyName = "MockFamily - 7" },
                new RevitFamilyDto { FamilyName = "MockFamily - 8" },
            };

            return comboItems;
        }

        public List<RevitColumnDto> GetColumn()
        {
            List<RevitColumnDto> comboItems = new List<RevitColumnDto>
            {
                new RevitColumnDto { ColumnName = "MockColumn - 1" },
                new RevitColumnDto { ColumnName = "MockColumn - 2" },
                new RevitColumnDto { ColumnName = "MockColumn - 3" },
                new RevitColumnDto { ColumnName = "MockColumn - 4" },
                new RevitColumnDto { ColumnName = "MockColumn - 5" },
                new RevitColumnDto { ColumnName = "MockColumn - 6" },
                new RevitColumnDto { ColumnName = "MockColumn - 7" },
                new RevitColumnDto { ColumnName = "MockColumn - 8" },
            };

            return comboItems;
        }
    }
}
