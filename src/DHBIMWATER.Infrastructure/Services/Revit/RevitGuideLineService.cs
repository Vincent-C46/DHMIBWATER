using DHBIMWATER.Application.DTOs.GuildeLine;
using DHBIMWATER.Application.Interfaces;

namespace DHBIMWATER.Infrastructure.Services.Revit
{
    public class RevitGuideLineService : IGuideLineService
    {
        // 실제로는 Revit API를 사용하여 데이터를 가져오는 로직이 여기에 포함
        public List<RevitFamilyDto> GetFamilies()
        {
            List<RevitFamilyDto> comboItems = new List<RevitFamilyDto>
            {
                new RevitFamilyDto { FamilyName = "RevitFamily - 1" },
                new RevitFamilyDto { FamilyName = "RevitFamily - 2" },
                new RevitFamilyDto { FamilyName = "RevitFamily - 3" },
                new RevitFamilyDto { FamilyName = "RevitFamily - 4" },
                new RevitFamilyDto { FamilyName = "RevitFamily - 5" },
                new RevitFamilyDto { FamilyName = "RevitFamily - 6" },
                new RevitFamilyDto { FamilyName = "RevitFamily - 7" },
                new RevitFamilyDto { FamilyName = "RevitFamily - 8" },
            };

            return comboItems;
        }

        public List<RevitColumnDto> GetColumn()
        {
           List<RevitColumnDto> comboItems = new List<RevitColumnDto>
            {
                new RevitColumnDto { ColumnName = "RevitColumn - 1" },
                new RevitColumnDto { ColumnName = "RevitColumn - 2" },
                new RevitColumnDto { ColumnName = "RevitColumn - 3" },
                new RevitColumnDto { ColumnName = "RevitColumn - 4" },
                new RevitColumnDto { ColumnName = "RevitColumn - 5" },
                new RevitColumnDto { ColumnName = "RevitColumn - 6" },
                new RevitColumnDto { ColumnName = "RevitColumn - 7" },
                new RevitColumnDto { ColumnName = "RevitColumn - 8" },
            };

            return comboItems;
        }
    }
}
