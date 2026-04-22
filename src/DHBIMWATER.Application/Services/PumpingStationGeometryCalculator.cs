using DHBIMWATER.Application.DTOs.Revit.PumpingStation;
using DHBIMWATER.Core.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Application.Services
{
    public static class PumpingStationGeometryCalculator
    {
        public static IReadOnlyList<LevelDefinition> CalculateLevels(PumpCreationRequestDto dto)
        {
            var d = dto.DesignConditionDto;
            var pr = dto.ProfileSpecDto;
            var pl = dto.PlanSpecDto;
            var ts = dto.TypeSelectionDto;

            double upperSlab = d.HWL * 1000 + pr.H3 + ts.T1;

            return new List<LevelDefinition>
              {
                  new LevelDefinition { Name = "기초(펌프)",  Elevation = d.LWL * 1000 - pr.H4 },
                  new LevelDefinition { Name = "기초(유입부)", Elevation = d.LWL * 1000 - pr.H1 },
                  new LevelDefinition { Name = "밸브실",      Elevation = upperSlab - pr.H7 - d.D - pr.H6 },
                  new LevelDefinition { Name = "상부슬래브",   Elevation = upperSlab },
              };
        }

        public static IReadOnlyList<SlabDefinition> CalculateSlabs(PumpCreationRequestDto dto)
        {
            var d = dto.DesignConditionDto;
            var pr = dto.ProfileSpecDto;
            var pl = dto.PlanSpecDto;
            var ts = dto.TypeSelectionDto;
            // 슬래브 계산 로직 추가 예정
            return new List<SlabDefinition>();
        }

        public static IReadOnlyList<LinearWallDefinition> CalculateWalls(PumpCreationRequestDto dto)
        {
            var d = dto.DesignConditionDto;
            var pr = dto.ProfileSpecDto;
            var pl = dto.PlanSpecDto;
            var ts = dto.TypeSelectionDto;

            // 벽 계산 로직 추가 예정
            return new List<LinearWallDefinition>();
        }
                public static IReadOnlyList<BeamDefinition> CalculateBemas(PumpCreationRequestDto dto)
        {
            var d = dto.DesignConditionDto;
            var pr = dto.ProfileSpecDto;
            var pl = dto.PlanSpecDto;
            var ts = dto.TypeSelectionDto;

            // 보 계산 로직 추가 예정
            return new List<BeamDefinition>();
        }
    }
}
