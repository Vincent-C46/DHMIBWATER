using DHBIMWATER.Application.DTOs.Revit.PumpingStation;
using DHBIMWATER.Core.Geometry;
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
            var slabs = new List<SlabDefinition>();
            var totalLength = pr.B1 + pl.B2 + pr.B3 + pr.B4 + pl.B5 + pr.B6 + ts.T3 + pr.B7 + ts.T4;
            var totalWidth = ts.T4 * 2 + (pl.B8 * d.N) + (ts.T5 * (d.N - 1));

            var upperSlabDef = new SlabDefinition
            {
                Thickness = ts.T1,
                ElevationZ = d.HWL * 1000 + pr.H3 + ts.T1,
                LevelName = "상부슬래브",
                ElementCode = "상부슬래브",
                Zone = "",
                Part = "",
            };
            var valveSlabDef = new SlabDefinition
            {
                Thickness = ts.T3,
                ElevationZ = upperSlabDef.ElevationZ - (pr.H7 + d.D + pr.H6),
                LevelName = "밸브실",
                ElementCode = "밸브실슬래브",
                Zone = "",
                Part = "",
            };

            switch (d.SelectedEntranceType)
            {
                case "좌안부":
                    upperSlabDef.Points = new List<Point2D>()
                    {
                        new Point2D(0, -ts.T4),
                        new Point2D(totalLength - (ts.T4 + pl.L5 + ts.T4 ) , -ts.T4),
                        new Point2D(totalLength - (ts.T4 + pl.L5 + ts.T4 ) , -ts.T5 - pl.B9 - ts.T4),
                        new Point2D(totalLength, -ts.T5 - pl.B9 - ts.T4),
                        new Point2D(totalLength, totalWidth - ts.T4),
                        new Point2D(0, totalWidth - ts.T4),
                    };

                    valveSlabDef.Points = new List<Point2D>()
                    {
                        new Point2D(totalLength - ts.T4 - pr.B7 - ts.T3, -ts.T5),
                        new Point2D(totalLength , -ts.T5),
                        new Point2D(totalLength , pl.B8 * d.N + ts.T5 * (d.N -1) + ts.T4 ),
                        new Point2D(totalLength - ts.T4 - pr.B7 - ts.T3, pl.B8 * d.N + ts.T5 * (d.N -1) + ts.T4 )
                    };
                    break;
                case "우안부":
                    upperSlabDef.Points = new List<Point2D>()
                    {
                        new Point2D(0, -ts.T4),
                        new Point2D(totalLength, - ts.T4),
                        new Point2D(totalLength, totalWidth - ts.T4 + ts.T5 + pl.B9 ),
                        new Point2D(totalLength- (ts.T4 + pl.L5 + ts.T4 ), totalWidth - ts.T4 + ts.T5 + pl.B9 ),
                        new Point2D(totalLength- (ts.T4 + pl.L5 + ts.T4 ), totalWidth  - ts.T4),
                        new Point2D(0, totalWidth - ts.T4),
                    };

                    valveSlabDef.Points = new List<Point2D>()
                    {
                        new Point2D(totalLength - ts.T4 - pr.B7 - ts.T3, -ts.T4),
                        new Point2D(totalLength , -ts.T4),
                        new Point2D(totalLength , pl.B8 * d.N + ts.T5 * (d.N -1) + ts.T5 ),
                        new Point2D(totalLength - ts.T4 - pr.B7 - ts.T3, pl.B8 * d.N + ts.T5 * (d.N -1) + ts.T5 )
                    };
                    break;
                case "측면부":
                    upperSlabDef.Points = new List<Point2D>()
                    {
                        new Point2D(0, -ts.T4),
                        new Point2D(totalLength, - ts.T4),
                        new Point2D(totalLength, totalWidth- ts.T4),
                        new Point2D(0, totalWidth - ts.T4),
                    };

                    valveSlabDef.Points = new List<Point2D>()
                    {
                        new Point2D(totalLength - ts.T4 - pr.B7 - ts.T3, -ts.T4),
                        new Point2D(totalLength , -ts.T4),
                        new Point2D(totalLength , pl.B8 * d.N + ts.T5 * (d.N -1) + ts.T4 ),
                        new Point2D(totalLength - ts.T4 - pr.B7 - ts.T3, pl.B8 * d.N + ts.T5 * (d.N -1) + ts.T4 )
                    };
                    break;
            }

            slabs.Add(upperSlabDef);
            slabs.Add(valveSlabDef);

            return slabs;
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
        public static IReadOnlyList<DirectShapeDefinition> CalculateDirectShapes(PumpCreationRequestDto dto)
        {
            var d = dto.DesignConditionDto;
            var pr = dto.ProfileSpecDto;
            var pl = dto.PlanSpecDto;
            var ts = dto.TypeSelectionDto;

            // 슬래브 계산 로직 추가 예정
            switch (d.SelectedEntranceType)
            {
                case "좌안부":
                    break;
                case "우안부":
                    break;
                case "측면부":
                    break;
            }

            return new List<DirectShapeDefinition>();
        }
    }
}
