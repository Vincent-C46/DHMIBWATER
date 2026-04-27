using DHBIMWATER.Application.DTOs.Revit.PumpingStation;
using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Structures;
using System.Net;
using System.Runtime.InteropServices;

namespace DHBIMWATER.Application.Services
{
    public class PumpingStationGeometryCalculator
    {
        private const string FoundationPumpLevelName = "기초(펌프)";
        private const string FoundationInletLevelName = "기초(유입부)";
        private const string ValveRoomLevelName = "밸브실";
        private const string UpperSlabLevelName = "상부슬래브";

        public static IReadOnlyList<LevelDefinition> CalculateLevels(PumpCreationRequestDto dto)
        {
            var d = dto.DesignConditionDto;
            var pr = dto.ProfileSpecDto;
            var pl = dto.PlanSpecDto;
            var ts = dto.TypeSelectionDto;

            double upperSlab = d.HWL * 1000 + pr.H3 + ts.T1;

            return new List<LevelDefinition>
              {
                  new LevelDefinition { Name = FoundationPumpLevelName,  Elevation = d.LWL * 1000 - pr.H4 },
                  new LevelDefinition { Name = FoundationInletLevelName, Elevation = d.LWL * 1000 - pr.H1 },
                  new LevelDefinition { Name = ValveRoomLevelName,       Elevation = upperSlab - pr.H7 - d.D - pr.H6 },
                  new LevelDefinition { Name = UpperSlabLevelName,       Elevation = upperSlab },
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
                LevelName = UpperSlabLevelName,
                ElementCode = UpperSlabLevelName,
                Zone = "",
                Part = "",
            };
            var valveSlabDef = new SlabDefinition
            {
                Thickness = ts.T3,
                ElevationZ = upperSlabDef.ElevationZ - (pr.H7 + d.D + pr.H6),
                LevelName = ValveRoomLevelName,
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
                        new Point2D(totalLength - ts.T4 - pr.B7 - ts.T3, 0),
                        new Point2D(totalLength - ts.T4 , 0),
                        new Point2D(totalLength - ts.T4 , pl.B8 * d.N + ts.T5 * (d.N -1) ),
                        new Point2D(totalLength - ts.T4 - pr.B7 - ts.T3, pl.B8 * d.N + ts.T5 * (d.N -1) )
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
                        new Point2D(totalLength - ts.T4 - pr.B7 - ts.T3, 0),
                        new Point2D(totalLength - ts.T4 , 0),
                        new Point2D(totalLength - ts.T4 , pl.B8 * d.N + ts.T5 * (d.N -1) ),
                        new Point2D(totalLength - ts.T4 - pr.B7 - ts.T3, pl.B8 * d.N + ts.T5 * (d.N -1) )
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
                        new Point2D(totalLength - ts.T4 - pr.B7 - ts.T3, 0),
                        new Point2D(totalLength - ts.T4 , 0),
                        new Point2D(totalLength - ts.T4 , pl.B8 * d.N + ts.T5 * (d.N -1) ),
                        new Point2D(totalLength - ts.T4 - pr.B7 - ts.T3, pl.B8 * d.N + ts.T5 * (d.N -1) )
                    };
                    break;
            }

            slabs.Add(upperSlabDef);
            slabs.Add(valveSlabDef);

            return slabs;
        }
        public static IReadOnlyList<LinearWallDefinition> CalculateLinearWalls(PumpCreationRequestDto dto)
        {
            var d = dto.DesignConditionDto;
            var pr = dto.ProfileSpecDto;
            var pl = dto.PlanSpecDto;
            var ts = dto.TypeSelectionDto;
            var totalLength = pr.B1 + pl.B2 + pr.B3 + pr.B4 + pl.B5 + pr.B6 + ts.T3 + pr.B7 + ts.T4;
            var totalWidth = ts.T4 * 2 + (pl.B8 * d.N) + (ts.T5 * (d.N - 1));

            var linearWalls = new List<LinearWallDefinition>();

            // Linear 벽 계산 로직 추가 예정
            switch (d.SelectedEntranceType)
            {
                case "좌안부":
                    if (d.SelectedPumpingStationType == "Type1")
                    {
                        // 와류방지벽 (Type1 공통)
                        for (int i = 0; i < d.N; i++)
                        {
                            var antiVortexWallDef = new LinearWallDefinition
                            {
                                Thickness = ts.T6,
                                Height = pr.H5 + ts.T1 - pr.H7 - d.D - pr.H6 - ts.T3,
                                BaseOffset = 0,
                                LevelName = FoundationPumpLevelName,
                                ElementCode = "",
                                Zone = "펌프장",
                                Part = "와류방지벽",
                            };

                            antiVortexWallDef.StartPoint = new Point3D(totalLength - ts.T4 - pr.B7 - ts.T3, pl.B8 / 2 + (pl.B8 + ts.T5) * i, 0);
                            antiVortexWallDef.EndPoint = new Point3D(totalLength - ts.T4, pl.B8 / 2 + (pl.B8 + ts.T5) * i, 0);

                            linearWalls.Add(antiVortexWallDef);
                        }

                        // 밸브실 하부 내벽 (Type1 공통)
                        for (int i = 0; i < d.N - 1; i++)
                        {
                            var innerWallUnderValveDef = new LinearWallDefinition
                            {
                                Thickness = ts.T5,
                                Height = pr.H5 + ts.T1 - pr.H7 - d.D - pr.H6 - ts.T3,
                                BaseOffset = 0,
                                LevelName = FoundationPumpLevelName,
                                ElementCode = "",
                                Zone = "펌프장",
                                Part = "펌프장 내벽",
                            };

                            innerWallUnderValveDef.StartPoint = new Point3D(totalLength - ts.T4 - pr.B7 - ts.T3, pl.B8 + ts.T5 / 2 + (pl.B8 + ts.T5) * i, 0);
                            innerWallUnderValveDef.EndPoint = new Point3D(totalLength - ts.T4, pl.B8 + ts.T5 / 2 + (pl.B8 + ts.T5) * i, 0);

                            linearWalls.Add(innerWallUnderValveDef);
                        }

                        // 좌안부 내벽 (좌안부, Type1, Type3 적용)
                        var innerEntranceWallDef = new LinearWallDefinition
                        {
                            Thickness = ts.T5,
                            Height = pr.H5,
                            BaseOffset = 0,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "",
                            Zone = "펌프장",
                            Part = "펌프장 내벽",
                        };
                        innerEntranceWallDef.StartPoint = new Point3D(totalLength - ts.T4 - pl.L5, -ts.T5 / 2, 0);
                        innerEntranceWallDef.EndPoint = new Point3D(totalLength - ts.T4, -ts.T5 / 2, 0);
                        linearWalls.Add(innerEntranceWallDef);

                        // 좌안부 외벽1 (좌안부, Type1) - 짧은 외벽
                        var outerWallDef1 = new LinearWallDefinition
                        {
                            Thickness = ts.T4,
                            Height = pr.H5,
                            BaseOffset = 0,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "",
                            Zone = "펌프장",
                            Part = "펌프장 외벽",
                        };
                        outerWallDef1.StartPoint = new Point3D(totalLength - ts.T4 - pl.L5 - ts.T4 / 2, -ts.T4, 0);
                        outerWallDef1.EndPoint = new Point3D(totalLength - ts.T4 - pl.L5 - ts.T4 / 2, -ts.T5 - pl.B9 - ts.T4, 0);
                        outerWallDef1.IsFlipped = true;
                        linearWalls.Add(outerWallDef1);

                        // 좌안부 외벽2 (좌안부, Type 무관)
                        var outerWallDef2 = new LinearWallDefinition
                        {
                            Thickness = ts.T4,
                            Height = pr.H5,
                            BaseOffset = 0,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "",
                            Zone = "펌프장",
                            Part = "펌프장 외벽",
                        };
                        outerWallDef2.StartPoint = new Point3D(totalLength - ts.T4 - pl.L5, -ts.T5 - pl.B9 - ts.T4 / 2, 0);
                        outerWallDef2.EndPoint = new Point3D(totalLength - ts.T4, -ts.T5 - pl.B9 - ts.T4 / 2, 0);
                        outerWallDef2.IsFlipped = true;
                        linearWalls.Add(outerWallDef2);

                        // 좌안부 외벽3 (좌안부, Type1 적용) - 긴 외벽
                        var outerWallDef3 = new LinearWallDefinition
                        {
                            Thickness = ts.T4,
                            Height = pr.H5,
                            BaseOffset = 0,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "",
                            Zone = "펌프장",
                            Part = "펌프장 외벽",
                        };
                        outerWallDef3.StartPoint = new Point3D(totalLength - ts.T4 / 2, -ts.T5 - pl.B9 - ts.T4, 0);
                        outerWallDef3.EndPoint = new Point3D(totalLength - ts.T4 / 2, totalWidth - ts.T4, 0);
                        outerWallDef3.IsFlipped = true;
                        linearWalls.Add(outerWallDef3);

                        // 좌안부 밸브실 사이벽
                        var valveRoomWallDef = new LinearWallDefinition
                        {
                            Thickness = ts.T3,
                            Height = pr.H7 + d.D + pr.H6 - ts.T1,
                            BaseOffset = 0,
                            LevelName = ValveRoomLevelName,
                            ElementCode = "",
                            Zone = "밸브실",
                            Part = "밸브실 사이벽",
                        };
                        valveRoomWallDef.StartPoint = new Point3D(totalLength - ts.T4 - pr.B7 - ts.T3 / 2, 0, 0);
                        valveRoomWallDef.EndPoint = new Point3D(totalLength - ts.T4 - pr.B7 - ts.T3 / 2, totalWidth - ts.T4 * 2, 0);
                        valveRoomWallDef.IsFlipped = true;
                        linearWalls.Add(valveRoomWallDef);

                        // 좌안부 외벽 - 긴 벽
                        var outerProfileWallDef1 = new ProfileWallDefinition
                        {
                            Thickness = ts.T4,
                            LevelName = FoundationPumpLevelName,
                        };
                        outerProfileWallDef1.Points = new List<Point3D>() {
                            new Point3D(0, 0, 0),
                            new Point3D(0, 100, 0),
                            new Point3D(0, 100, 100),
                        };
                    }
                    else if (d.SelectedPumpingStationType == "Type2")
                    {
                        // 좌안부 Type2 벽체 계산 로직
                    }
                    else if (d.SelectedPumpingStationType == "Type3")
                    {
                        // 좌안부 Type3 벽체 계산 로직
                    }
                    break;
                case "우안부":
                    if (d.SelectedPumpingStationType == "Type1")
                    {
                        // 우안부 Type1 벽체 계산 로직
                    }
                    else if (d.SelectedPumpingStationType == "Type2")
                    {
                        // 우안부 Type2 벽체 계산 로직
                    }
                    else if (d.SelectedPumpingStationType == "Type3")
                    {
                        // 우안부 Type3 벽체 계산 로직
                    }
                    break;
                case "측면부":
                    if (d.SelectedPumpingStationType == "Type1")
                    {
                        // 측면부 Type1 벽체 계산 로직
                    }
                    else if (d.SelectedPumpingStationType == "Type2")
                    {
                        // 측면부 Type2 벽체 계산 로직
                    }
                    else if (d.SelectedPumpingStationType == "Type3")
                    {
                        // 측면부 Type3 벽체 계산 로직
                    }
                    break;
            }

            return linearWalls;
        }

        public static IReadOnlyList<ProfileWallDefinition> CalculateProfileWalls(PumpCreationRequestDto dto)
        {
            var d = dto.DesignConditionDto;
            var pr = dto.ProfileSpecDto;
            var pl = dto.PlanSpecDto;
            var ts = dto.TypeSelectionDto;
            var totalLength = pr.B1 + pl.B2 + pr.B3 + pr.B4 + pl.B5 + pr.B6 + ts.T3 + pr.B7 + ts.T4;
            var totalWidth = ts.T4 * 2 + (pl.B8 * d.N) + (ts.T5 * (d.N - 1));

            var profileWalls = new List<ProfileWallDefinition>();

            // Profile 벽 계산 로직 추가 예정
            switch (d.SelectedEntranceType)
            {
                case "좌안부":
                    // 경사부 직전까지 거리
                    double x2 = totalLength - ts.T4 - pr.B7 - ts.T3 - pr.B6 - pl.B5 / 2 - pr.L4 - pr.L3;

                    if (d.SelectedPumpingStationType == "Type1")
                    {
                        // 좌안부 외벽 - 진입부측 프로파일 (짧은 벽체)
                        var outerProfileWallDef1 = new ProfileWallDefinition
                        {
                            Thickness = ts.T4,
                            LevelName = FoundationPumpLevelName,
                        };
                        outerProfileWallDef1.Points = new List<Point3D>() {
                            new Point3D(0, -ts.T4/2, d.LWL * 1000 - pr.H1),
                            new Point3D(x2, -ts.T4/2, d.LWL * 1000 - pr.H1),
                            new Point3D(x2 + pr.L3, -ts.T4/2,  d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - ts.T4 - pl.L5, -ts.T4/2, d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - ts.T4 - pl.L5, -ts.T4/2, d.HWL * 1000 + pr.H3),
                            new Point3D(0, -ts.T4/2, d.HWL * 1000 + pr.H3),
                        };
                        profileWalls.Add(outerProfileWallDef1);

                        // 좌안부 외벽 - 진입부 반대측 프로파일 (긴 벽체, 공통)
                        var outerProfileWallDef2 = new ProfileWallDefinition
                        {
                            Thickness = ts.T4,
                            LevelName = FoundationPumpLevelName,
                        };
                        outerProfileWallDef2.Points = new List<Point3D>() {
                            new Point3D(0, pl.B8 * d.N + ts.T5 * (d.N-1) + ts.T4/2, d.LWL * 1000 - pr.H1),
                            new Point3D(x2, pl.B8 * d.N + ts.T5 * (d.N-1) + ts.T4/2, d.LWL * 1000 - pr.H1),
                            new Point3D(x2 + pr.L3, pl.B8 * d.N + ts.T5 * (d.N-1) + ts.T4/2,  d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - ts.T4, pl.B8 * d.N + ts.T5 * (d.N-1) + ts.T4/2, d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - ts.T4, pl.B8 * d.N + ts.T5 * (d.N-1) + ts.T4/2, d.HWL * 1000 + pr.H3),
                            new Point3D(0, pl.B8 * d.N + ts.T5 * (d.N-1) + ts.T4/2, d.HWL * 1000 + pr.H3),
                        };
                        profileWalls.Add(outerProfileWallDef2);

                        // 지 사이 내벽 (공통)
                        for(int i = 0; i< d.N - 1; i++)
                        {
                            var innerProfileWallDef = new ProfileWallDefinition
                            {
                                Thickness = ts.T5,
                                LevelName = FoundationPumpLevelName,
                            };
                            innerProfileWallDef.Points = new List<Point3D>() {
                            new Point3D(0, -ts.T5/2 + (pl.B8 + ts.T5)*(i+1), d.LWL * 1000 - pr.H1),
                            new Point3D(x2, -ts.T5/2 + (pl.B8 + ts.T5)*(i+1), d.LWL * 1000 - pr.H1),
                            new Point3D(x2 + pr.L3, -ts.T5/2 + (pl.B8 + ts.T5)*(i+1),  d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - ts.T4 - pr.B7 - ts.T3, -ts.T5/2 + (pl.B8 + ts.T5)*(i+1), d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - ts.T4 - pr.B7 - ts.T3, -ts.T5/2 + (pl.B8 + ts.T5)*(i+1), d.HWL * 1000 + pr.H3),
                            new Point3D(0, -ts.T5/2 + (pl.B8 + ts.T5)*(i+1), d.HWL * 1000 + pr.H3),
                        };
                            profileWalls.Add(innerProfileWallDef);

                        }
                    }
                    else if (d.SelectedPumpingStationType == "Type2")
                    {
                        // 좌안부 Type2 벽체 계산 로직
                    }
                    else if (d.SelectedPumpingStationType == "Type3")
                    {
                        // 좌안부 Type3 벽체 계산 로직
                    }
                    break;
                case "우안부":
                    if (d.SelectedPumpingStationType == "Type1")
                    {
                        // 우안부 Type1 벽체 계산 로직
                    }
                    else if (d.SelectedPumpingStationType == "Type2")
                    {
                        // 우안부 Type2 벽체 계산 로직
                    }
                    else if (d.SelectedPumpingStationType == "Type3")
                    {
                        // 우안부 Type3 벽체 계산 로직
                    }
                    break;
                case "측면부":
                    if (d.SelectedPumpingStationType == "Type1")
                    {
                        // 측면부 Type1 벽체 계산 로직
                    }
                    else if (d.SelectedPumpingStationType == "Type2")
                    {
                        // 측면부 Type2 벽체 계산 로직
                    }
                    else if (d.SelectedPumpingStationType == "Type3")
                    {
                        // 측면부 Type3 벽체 계산 로직
                    }
                    break;
            }

            return profileWalls;
        }

        public static IReadOnlyList<BeamDefinition> CalculateBeams(PumpCreationRequestDto dto)
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
