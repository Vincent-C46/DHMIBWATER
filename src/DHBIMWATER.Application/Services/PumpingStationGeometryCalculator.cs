using DHBIMWATER.Application.DTOs.Revit.PumpingStation;
using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Structures;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;

namespace DHBIMWATER.Application.Services
{
    public class PumpingStationGeometryCalculator
    {
        private const string FoundationPumpLevelName = "기초(펌프)";
        private const string FoundationInletLevelName = "기초(유입부)";
        private const string ValveRoomLevelName = "밸브실";
        private const string UpperSlabLevelName = "상부슬래브";

        private readonly PumpCreationRequestDto dto;
        private readonly double totalLength;
        private readonly double totalWidth;
        private readonly double x2;

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
                ElementCode = "S1",
                Zone = "",
                Part = "상부슬래브",
            };
            var valveSlabDef = new SlabDefinition
            {
                Thickness = ts.T3,
                ElevationZ = upperSlabDef.ElevationZ - (pr.H7 + d.D + pr.H6),
                LevelName = ValveRoomLevelName,
                ElementCode = "MS1",
                Zone = "",
                Part = "밸브실슬래브",
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

                    if (d.SelectedPumpingStationType == "Type2")
                    {
                        valveSlabDef.Points = new List<Point2D>()
                        {
                        new Point2D(totalLength - ts.T4 - pr.B7 - ts.T3, 0),
                        new Point2D(totalLength, 0),
                        new Point2D(totalLength, pl.B8 * d.N + ts.T5 * (d.N -1) + ts.T4),
                        new Point2D(totalLength - ts.T4 - pr.B7 - ts.T3, pl.B8 * d.N + ts.T5 * (d.N -1)+ ts.T4 )
                        };
                    }
                    else
                    {
                        valveSlabDef.Points = new List<Point2D>()
                        {
                        new Point2D(totalLength - ts.T4 - pr.B7 - ts.T3, 0),
                        new Point2D(totalLength - ts.T4 , 0),
                        new Point2D(totalLength - ts.T4 , pl.B8 * d.N + ts.T5 * (d.N -1) ),
                        new Point2D(totalLength - ts.T4 - pr.B7 - ts.T3, pl.B8 * d.N + ts.T5 * (d.N -1) )
                        };
                    }

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
                    if (d.SelectedPumpingStationType == "Type2")
                    {
                        valveSlabDef.Points = new List<Point2D>()
                    {
                        new Point2D(totalLength - ts.T4 - pr.B7 - ts.T3, 0),
                        new Point2D(totalLength - ts.T4 , 0),
                        new Point2D(totalLength - ts.T4 , pl.B8 * d.N + ts.T5 * (d.N -1) ),
                        new Point2D(totalLength - ts.T4 - pr.B7 - ts.T3, pl.B8 * d.N + ts.T5 * (d.N -1) )
                    };
                    }
                    else
                    {
                        valveSlabDef.Points = new List<Point2D>()
                        {
                        new Point2D(totalLength - ts.T4 - pr.B7 - ts.T3, -ts.T4),
                        new Point2D(totalLength, -ts.T4),
                        new Point2D(totalLength, pl.B8 * d.N + ts.T5 *(d.N - 1) + ts.T4 ),
                        new Point2D(totalLength - ts.T4 - pr.B7 - ts.T3, pl.B8 * d.N + ts.T5 *(d.N - 1) + ts.T4 )
                        };
                    }
                    break;
                case "측면부":
                    upperSlabDef.Points = new List<Point2D>()
                    {
                        new Point2D(0, -ts.T4),
                        new Point2D(totalLength, - ts.T4),
                        new Point2D(totalLength, totalWidth- ts.T4),
                        new Point2D(0, totalWidth - ts.T4),
                    };
                    if (d.SelectedPumpingStationType == "Type2")
                    {
                        valveSlabDef.Points = new List<Point2D>()
                        {
                            new Point2D(totalLength - ts.T4 - pr.B7 - ts.T3, -ts.T4),
                            new Point2D(totalLength  , -ts.T4),
                            new Point2D(totalLength , pl.B8 * d.N + ts.T5 * (d.N -1)  +ts.T4),
                            new Point2D(totalLength - ts.T4 - pr.B7 - ts.T3, pl.B8 * d.N + ts.T5 * (d.N -1)  + ts.T4)
                        };
                    }
                    else
                    {
                        valveSlabDef.Points = new List<Point2D>()
                        {
                            new Point2D(totalLength - ts.T4 - pr.B7 - ts.T3, 0),
                            new Point2D(totalLength - ts.T4 , 0),
                            new Point2D(totalLength - ts.T4 , pl.B8 * d.N + ts.T5 * (d.N -1) ),
                            new Point2D(totalLength - ts.T4 - pr.B7 - ts.T3, pl.B8 * d.N + ts.T5 * (d.N -1) )
                        };
                    }

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
                                ElementCode = "AVW",
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
                                ElementCode = "W3-1",
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
                            ElementCode = "W5",
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
                            ElementCode = "W1-2",
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
                            ElementCode = "W1-3",
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
                            ElementCode = "W2",
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
                            ElementCode = "W4",
                            Zone = "밸브실",
                            Part = "밸브실 사이벽",
                        };
                        valveRoomWallDef.StartPoint = new Point3D(totalLength - ts.T4 - pr.B7 - ts.T3 / 2, 0, 0);
                        valveRoomWallDef.EndPoint = new Point3D(totalLength - ts.T4 - pr.B7 - ts.T3 / 2, totalWidth - ts.T4 * 2, 0);
                        valveRoomWallDef.IsFlipped = true;
                        linearWalls.Add(valveRoomWallDef);
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
                        // 와류방지벽 (Type1 공통)
                        for (int i = 0; i < d.N; i++)
                        {
                            var antiVortexWallDef = new LinearWallDefinition
                            {
                                Thickness = ts.T6,
                                Height = pr.H5 + ts.T1 - pr.H7 - d.D - pr.H6 - ts.T3,
                                BaseOffset = 0,
                                LevelName = FoundationPumpLevelName,
                                ElementCode = "AVW",
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
                                ElementCode = "W3-1",
                                Zone = "펌프장",
                                Part = "펌프장 내벽",
                            };

                            innerWallUnderValveDef.StartPoint = new Point3D(totalLength - ts.T4 - pr.B7 - ts.T3, pl.B8 + ts.T5 / 2 + (pl.B8 + ts.T5) * i, 0);
                            innerWallUnderValveDef.EndPoint = new Point3D(totalLength - ts.T4, pl.B8 + ts.T5 / 2 + (pl.B8 + ts.T5) * i, 0);

                            linearWalls.Add(innerWallUnderValveDef);
                        }
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
                        // 와류방지벽 (Type1 공통)
                        for (int i = 0; i < d.N; i++)
                        {
                            var antiVortexWallDef = new LinearWallDefinition
                            {
                                Thickness = ts.T6,
                                Height = pr.H5 + ts.T1 - pr.H7 - d.D - pr.H6 - ts.T3,
                                BaseOffset = 0,
                                LevelName = FoundationPumpLevelName,
                                ElementCode = "AVW",
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
                                ElementCode = "W3-1",
                                Zone = "펌프장",
                                Part = "펌프장 내벽",
                            };

                            innerWallUnderValveDef.StartPoint = new Point3D(totalLength - ts.T4 - pr.B7 - ts.T3, pl.B8 + ts.T5 / 2 + (pl.B8 + ts.T5) * i, 0);
                            innerWallUnderValveDef.EndPoint = new Point3D(totalLength - ts.T4, pl.B8 + ts.T5 / 2 + (pl.B8 + ts.T5) * i, 0);

                            linearWalls.Add(innerWallUnderValveDef);
                        }
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
            double x2 = totalLength - ts.T4 - pr.B7 - ts.T3 - pr.B6 - pl.B5 / 2 - pr.L4 - pr.L3;

            var profileWalls = new List<ProfileWallDefinition>();

            // 지 사이 내벽 (공통)
            for (int i = 0; i < d.N - 1; i++)
            {
                var innerProfileWallDef = new ProfileWallDefinition
                {
                    Thickness = ts.T5,
                    LevelName = FoundationPumpLevelName,
                    ElementCode = "W3",
                    Zone = "",
                    Part = ""
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

            // Profile 벽 계산 로직 추가 예정
            switch (d.SelectedEntranceType)
            {
                case "좌안부":
                    if (d.SelectedPumpingStationType == "Type1")
                    {
                        // 좌안부 외벽 - 진입부측 프로파일 (짧은 벽체)
                        var outerProfileWallDef1 = new ProfileWallDefinition
                        {
                            Thickness = ts.T4,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "W1-1",
                            Zone = "",
                            Part = ""
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
                            ElementCode = "W1",
                            Zone = "",
                            Part = ""
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
            var totalLength = pr.B1 + pl.B2 + pr.B3 + pr.B4 + pl.B5 + pr.B6 + ts.T3 + pr.B7 + ts.T4;
            var totalWidth = ts.T4 * 2 + (pl.B8 * d.N) + (ts.T5 * (d.N - 1));
            double x2 = totalLength - ts.T4 - pr.B7 - ts.T3 - pr.B6 - pl.B5 / 2 - pr.L4 - pr.L3;

            var beamDefs = new List<BeamDefinition>();
            for (int i = 0; i < d.N; i++)
            {
                var beamDef1 = new BeamDefinition()
                {
                    StartPoint = new Point3D(pr.B1 + pl.B2 + ts.GB1 / 2, (pl.B8 + ts.T5) * i, d.HWL * 1000 + pr.H3 + ts.T1 - ts.GH1 / 2),
                    EndPoint = new Point3D(pr.B1 + pl.B2 + ts.GB1 / 2, (pl.B8 + ts.T5) * i + pl.B8, d.HWL * 1000 + pr.H3 + ts.T1 - ts.GH1 / 2),
                    Width = ts.GB1,
                    Height = ts.GH1,
                    LevelName = UpperSlabLevelName,

                    ElementCode = "",
                    Zone = "",
                    Part = "",
                };
                var beamDef2 = new BeamDefinition()
                {
                    StartPoint = new Point3D(pr.B1 + pl.B2 + pr.B3 - ts.GB1 / 2, (pl.B8 + ts.T5) * i, d.HWL * 1000 + pr.H3 + ts.T1 - ts.GH1 / 2),
                    EndPoint = new Point3D(pr.B1 + pl.B2 + pr.B3 - ts.GB1 / 2, (pl.B8 + ts.T5) * i + pl.B8, d.HWL * 1000 + pr.H3 + ts.T1 - ts.GH1 / 2),
                    Width = ts.GB1,
                    Height = ts.GH1,
                    LevelName = UpperSlabLevelName,

                    ElementCode = "",
                    Zone = "",
                    Part = "",
                };
                var beamDef3 = new BeamDefinition()
                {
                    StartPoint = new Point3D(pr.B1 + pl.B2 + pr.B3 + pr.B4 - ts.GB1 / 2, (pl.B8 + ts.T5) * i, d.HWL * 1000 + pr.H3 + ts.T1 - ts.GH1 / 2),
                    EndPoint = new Point3D(pr.B1 + pl.B2 + pr.B3 + pr.B4 - ts.GB1 / 2, (pl.B8 + ts.T5) * i + pl.B8, d.HWL * 1000 + pr.H3 + ts.T1 - ts.GH1 / 2),
                    Width = ts.GB1,
                    Height = ts.GH1,
                    LevelName = UpperSlabLevelName,

                    ElementCode = "",
                    Zone = "",
                    Part = "",
                };

                beamDefs.Add(beamDef1);
                beamDefs.Add(beamDef2);
                beamDefs.Add(beamDef3);
            }

            return beamDefs;
        }
        public static IReadOnlyList<SolidExtrusionDefinition> CalculateSolids(PumpCreationRequestDto dto)
        {
            var d = dto.DesignConditionDto;
            var pr = dto.ProfileSpecDto;
            var pl = dto.PlanSpecDto;
            var ts = dto.TypeSelectionDto;

            var totalLength = pr.B1 + pl.B2 + pr.B3 + pr.B4 + pl.B5 + pr.B6 + ts.T3 + pr.B7 + ts.T4;
            var totalWidth = ts.T4 * 2 + (pl.B8 * d.N) + (ts.T5 * (d.N - 1));
            double x2 = totalLength - ts.T4 - pr.B7 - ts.T3 - pr.B6 - pl.B5 / 2 - pr.L4 - pr.L3;
            double subThk = 100; // 버림 두께 (임시값, 추후 설계조건에 따라 조정 필요)

            var solidExtrusionDefs = new List<SolidExtrusionDefinition>();

            // 슬래브 계산 로직 추가 예정
            var calculatedTheta = Math.Atan((pr.H4 - pr.H1) / pr.L3);
            var fndBaseSolid = new SolidExtrusionDefinition();
            fndBaseSolid.ElementCode = "F1";
            fndBaseSolid.Zone = "";
            fndBaseSolid.Part = "기초슬래브";
            fndBaseSolid.Normal = new Vector3D(0, 1, 0);
            fndBaseSolid.Distance = totalWidth + 2 * pl.B10;

            // 공통 - 버림
            var subBaseSolid = new SolidExtrusionDefinition();
            subBaseSolid.ElementCode = "F2";
            subBaseSolid.Zone = "";
            subBaseSolid.Part = "기초버림슬래브";
            subBaseSolid.Normal = new Vector3D(0, 1, 0);
            subBaseSolid.Distance = totalWidth + 2 * (pl.B10 + subThk);

            switch (d.SelectedPumpingStationType)
            {
                case "Type1":
                    fndBaseSolid.Profile = new List<Point3D>()
                                            {
                                                new Point3D(0,                                                  -ts.T4 - pl.B10, d.LWL*1000 - pr.H1),
                                                new Point3D(x2,                                                 -ts.T4 - pl.B10, d.LWL*1000 - pr.H1),
                                                new Point3D(x2 + pr.L3,                                         -ts.T4 - pl.B10, d.LWL*1000 - pr.H4),
                                                new Point3D(totalLength + pl.B10,                               -ts.T4 - pl.B10, d.LWL*1000 - pr.H4),
                                                new Point3D(totalLength + pl.B10,                               -ts.T4 - pl.B10, d.LWL*1000 - pr.H4 - ts.T2),
                                                new Point3D(x2 + pr.L3 - ts.T2 * Math.Tan(calculatedTheta / 2), -ts.T4 - pl.B10, d.LWL*1000 - pr.H4 - ts.T2),
                                                new Point3D(x2 - ts.T2 * Math.Tan(calculatedTheta / 2),         -ts.T4 - pl.B10, d.LWL*1000 - pr.H1 - ts.T2),
                                                new Point3D(0,                                                  -ts.T4 - pl.B10, d.LWL*1000 - pr.H1 - ts.T2),
                                            };
                    subBaseSolid.Profile = new List<Point3D>()
                                            {
                                                new Point3D(- subThk,                                                  -ts.T4 - pl.B10- subThk, d.LWL*1000 - pr.H1 - ts.T2),
                                                new Point3D(x2 - ts.T2 * Math.Tan(calculatedTheta / 2),                                                 -ts.T4 - pl.B10- subThk, d.LWL*1000 - pr.H1 - ts.T2),
                                                new Point3D(x2 + pr.L3 - ts.T2 * Math.Tan(calculatedTheta / 2),                                         -ts.T4 - pl.B10- subThk, d.LWL*1000 - pr.H4- ts.T2),
                                                new Point3D(totalLength + pl.B10 + subThk,                               -ts.T4 - pl.B10- subThk, d.LWL*1000 - pr.H4- ts.T2),
                                                new Point3D(totalLength + pl.B10 + subThk,                               -ts.T4 - pl.B10- subThk, d.LWL*1000 - pr.H4 - ts.T2 - subThk),
                                                new Point3D(x2 + pr.L3 - (ts.T2 + subThk) * Math.Tan(calculatedTheta / 2), -ts.T4 - pl.B10- subThk, d.LWL*1000 - pr.H4 - ts.T2- subThk),
                                                new Point3D(x2 - (ts.T2 + subThk) * Math.Tan(calculatedTheta / 2),         -ts.T4 - pl.B10- subThk, d.LWL*1000 - pr.H1 - ts.T2- subThk),
                                                new Point3D(- subThk,                                                  -ts.T4 - pl.B10- subThk, d.LWL*1000 - pr.H1 - ts.T2- subThk),
                                            };
                    break;
                case "Type2":
                    fndBaseSolid.Profile = new List<Point3D>()
                                            {
                                                new Point3D(0,                                                  -ts.T4 - pl.B10, d.LWL*1000 - pr.H1),
                                                new Point3D(x2,                                                 -ts.T4 - pl.B10, d.LWL*1000 - pr.H1),
                                                new Point3D(x2 + pr.L3,                                         -ts.T4 - pl.B10, d.LWL*1000 - pr.H4),
                                                new Point3D(totalLength - ts.T4 - pr.B7,                        -ts.T4 - pl.B10, d.LWL*1000 - pr.H4),
                                                new Point3D(totalLength - ts.T4 - pr.B7,                        -ts.T4 - pl.B10, d.LWL*1000 - pr.H4 - ts.T2),
                                                new Point3D(x2 + pr.L3 - ts.T2 * Math.Tan(calculatedTheta / 2), -ts.T4 - pl.B10, d.LWL*1000 - pr.H4 - ts.T2),
                                                new Point3D(x2 - ts.T2 * Math.Tan(calculatedTheta / 2),         -ts.T4 - pl.B10, d.LWL*1000 - pr.H1 - ts.T2),
                                                new Point3D(0,                                                  -ts.T4 - pl.B10, d.LWL*1000 - pr.H1 - ts.T2),
                                            };

                    subBaseSolid.Profile = new List<Point3D>()
                                            {
                                                new Point3D(- subThk,                                                      -ts.T4 - pl.B10- subThk, d.LWL*1000 - pr.H1 - ts.T2),
                                                new Point3D(x2 - ts.T2 * Math.Tan(calculatedTheta / 2),                    -ts.T4 - pl.B10- subThk, d.LWL*1000 - pr.H1 - ts.T2),
                                                new Point3D(x2 + pr.L3 - ts.T2 * Math.Tan(calculatedTheta / 2),            -ts.T4 - pl.B10- subThk, d.LWL*1000 - pr.H4- ts.T2),
                                                new Point3D(totalLength - ts.T4 - pr.B7 + subThk,                          -ts.T4 - pl.B10- subThk, d.LWL*1000 - pr.H4- ts.T2),
                                                new Point3D(totalLength - ts.T4 - pr.B7 + subThk,                          -ts.T4 - pl.B10- subThk, d.LWL*1000 - pr.H4 - ts.T2 - subThk),
                                                new Point3D(x2 + pr.L3 - (ts.T2 + subThk) * Math.Tan(calculatedTheta / 2), -ts.T4 - pl.B10- subThk, d.LWL*1000 - pr.H4 - ts.T2- subThk),
                                                new Point3D(x2 - (ts.T2 + subThk) * Math.Tan(calculatedTheta / 2),         -ts.T4 - pl.B10- subThk, d.LWL*1000 - pr.H1 - ts.T2- subThk),
                                                new Point3D(- subThk,                                                      -ts.T4 - pl.B10- subThk, d.LWL*1000 - pr.H1 - ts.T2- subThk),
                                            };
                    break;
                case "Type3":
                    fndBaseSolid.Profile = new List<Point3D>()
                                            {
                                                new Point3D(0,                                                  -ts.T4 - pl.B10, d.LWL*1000 - pr.H1),
                                                new Point3D(x2,                                                 -ts.T4 - pl.B10, d.LWL*1000 - pr.H1),
                                                new Point3D(x2 + pr.L3,                                         -ts.T4 - pl.B10, d.LWL*1000 - pr.H4),
                                                new Point3D(totalLength + pl.B10,                               -ts.T4 - pl.B10, d.LWL*1000 - pr.H4),
                                                new Point3D(totalLength + pl.B10,                               -ts.T4 - pl.B10, d.LWL*1000 - pr.H4 - ts.T2),
                                                new Point3D(x2 + pr.L3 - ts.T2 * Math.Tan(calculatedTheta / 2), -ts.T4 - pl.B10, d.LWL*1000 - pr.H4 - ts.T2),
                                                new Point3D(x2 - ts.T2 * Math.Tan(calculatedTheta / 2),         -ts.T4 - pl.B10, d.LWL*1000 - pr.H1 - ts.T2),
                                                new Point3D(0,                                                  -ts.T4 - pl.B10, d.LWL*1000 - pr.H1 - ts.T2),
                                            };

                    subBaseSolid.Profile = new List<Point3D>()
                                            {
                                                new Point3D(- subThk,                                                  -ts.T4 - pl.B10- subThk, d.LWL*1000 - pr.H1 - ts.T2),
                                                new Point3D(x2 - ts.T2 * Math.Tan(calculatedTheta / 2),                                                 -ts.T4 - pl.B10- subThk, d.LWL*1000 - pr.H1 - ts.T2),
                                                new Point3D(x2 + pr.L3 - ts.T2 * Math.Tan(calculatedTheta / 2),                                         -ts.T4 - pl.B10- subThk, d.LWL*1000 - pr.H4- ts.T2),
                                                new Point3D(totalLength + pl.B10 + subThk,                               -ts.T4 - pl.B10- subThk, d.LWL*1000 - pr.H4- ts.T2),
                                                new Point3D(totalLength + pl.B10 + subThk,                               -ts.T4 - pl.B10- subThk, d.LWL*1000 - pr.H4 - ts.T2 - subThk),
                                                new Point3D(x2 + pr.L3 - (ts.T2 + subThk) * Math.Tan(calculatedTheta / 2), -ts.T4 - pl.B10- subThk, d.LWL*1000 - pr.H4 - ts.T2- subThk),
                                                new Point3D(x2 - (ts.T2 + subThk) * Math.Tan(calculatedTheta / 2),         -ts.T4 - pl.B10- subThk, d.LWL*1000 - pr.H1 - ts.T2- subThk),
                                                new Point3D(- subThk,                                                  -ts.T4 - pl.B10- subThk, d.LWL*1000 - pr.H1 - ts.T2- subThk),
                                            };

                    break;
            }

            solidExtrusionDefs.Add(fndBaseSolid);
            solidExtrusionDefs.Add(subBaseSolid);

            // 공통 - 기초 경사부 계단 및 유입부 턱
            double threadWidth = pr.L3 / pr.NS;
            double riserHeight = pr.HS;

            for (int i = 0; i < d.N; i++)
            {
                var stairPts = new List<Point3D>();
                for (int j = 0; j < pr.NS; j++)
                {
                    stairPts.Add(new Point3D(x2 + threadWidth * j, (pl.B8 + ts.T5) * i, d.LWL * 1000 - pr.H1 - riserHeight * j));
                    stairPts.Add(new Point3D(x2 + threadWidth * (j + 1), (pl.B8 + ts.T5) * i, d.LWL * 1000 - pr.H1 - riserHeight * j));
                }
                stairPts.Add(new Point3D(x2 + threadWidth * pr.NS, (pl.B8 + ts.T5) * i, d.LWL * 1000 - pr.H4 - 100));   // CurveLoop 오류 막기위해 마지막 100mm 여유

                var fndStairs = new SolidExtrusionDefinition
                {
                    Profile = stairPts,
                    Normal = new Vector3D(0, 1, 0),
                    Distance = pl.B8,
                    ElementCode = "F1",
                    Zone = "",
                    Part = "기초 계단",
                };

                solidExtrusionDefs.Add(fndStairs);

                var inletCurb = new SolidExtrusionDefinition
                {
                    Profile = new List<Point3D>()
                                    {
                                        new Point3D(0,              (pl.B8 + ts.T5)* i, d.LWL*1000),
                                        new Point3D(pr.L1,          (pl.B8 + ts.T5)* i, d.LWL*1000),
                                        new Point3D(pr.L1 + pr.L2,  (pl.B8 + ts.T5)* i, d.LWL*1000 - pr.H1),
                                        new Point3D(0,              (pl.B8 + ts.T5)* i, d.LWL*1000 - pr.H1),
                                    },
                    Normal = new Vector3D(0, 1, 0),
                    Distance = pl.B8,
                    ElementCode = "F1",
                    Zone = "",
                    Part = "유입부 턱",
                };

                solidExtrusionDefs.Add(inletCurb);
            }

            switch (d.SelectedEntranceType)
            {
                case "좌안부":
                    var leftFndDef = new SolidExtrusionDefinition
                    {
                        Profile = new List<Point3D>()
                        {
                            new Point3D(totalLength + pl.B10,                     pl.B10, d.LWL*1000 - pr.H4),
                            new Point3D(totalLength + pl.B10,                     -ts.T5 - pl.B9 - ts.T4 - pl.B10, d.LWL*1000 - pr.H4),
                            new Point3D(totalLength - ts.T4 * 2 - pl.L5 - pl.B10, -ts.T5 - pl.B9 - ts.T4 - pl.B10, d.LWL*1000 - pr.H4),
                            new Point3D(totalLength - ts.T4 * 2 - pl.L5 - pl.B10, pl.B10, d.LWL*1000 - pr.H4),
                        },
                        Normal = new Vector3D(0, 0, -1),
                        Distance = ts.T2,
                        ElementCode = "F1",
                        Zone = "",
                        Part = "",
                    };
                    solidExtrusionDefs.Add(leftFndDef);

                    var leftSubFndDef = new SolidExtrusionDefinition
                    {
                        Profile = new List<Point3D>()
                        {
                            new Point3D(totalLength + pl.B10 + subThk,                     pl.B10 + subThk, d.LWL*1000 - ts.T2 - pr.H4),
                            new Point3D(totalLength + pl.B10 + subThk,                     -ts.T5 - pl.B9 - ts.T4 - pl.B10 - subThk, d.LWL*1000 - ts.T2 - pr.H4),
                            new Point3D(totalLength - ts.T4 * 2 - pl.L5 - pl.B10 - subThk, -ts.T5 - pl.B9 - ts.T4 - pl.B10 - subThk, d.LWL*1000 - ts.T2 - pr.H4),
                            new Point3D(totalLength - ts.T4 * 2 - pl.L5 - pl.B10 - subThk, pl.B10 + subThk, d.LWL*1000 - ts.T2 - pr.H4),
                        },
                        Normal = new Vector3D(0, 0, -1),
                        Distance = subThk,
                        ElementCode = "F2",
                        Zone = "",
                        Part = "",
                    };
                    solidExtrusionDefs.Add(leftSubFndDef);

                    break;
                case "우안부":
                    break;
                case "측면부":
                    break;
            }



            return solidExtrusionDefs;
        }
        public static IReadOnlyList<RectangularSlabOpeningDefinition> CalculateRectangularSlabOpenings(PumpCreationRequestDto dto)
        {
            var d = dto.DesignConditionDto;
            var pr = dto.ProfileSpecDto;
            var pl = dto.PlanSpecDto;
            var ts = dto.TypeSelectionDto;
            var totalLength = pr.B1 + pl.B2 + pr.B3 + pr.B4 + pl.B5 + pr.B6 + ts.T3 + pr.B7 + ts.T4;
            var totalWidth = ts.T4 * 2 + (pl.B8 * d.N) + (ts.T5 * (d.N - 1));
            double x2 = totalLength - ts.T4 - pr.B7 - ts.T3 - pr.B6 - pl.B5 / 2 - pr.L4 - pr.L3;

            var openings = new List<RectangularSlabOpeningDefinition>();

            for (int i = 0; i < d.N; i++)
            {

                if (pl.IsRectangularOpening)
                {
                    var pumpOpening = new RectangularSlabOpeningDefinition
                    {
                        Width = pl.B5,
                        Length = pl.B5,
                        Position = new Point2D(totalLength - ts.T4 - pr.B7 - ts.T3 - pr.B6 - pl.B5 / 2, pl.B8 / 2 + (pl.B8 + ts.T5) * i),

                        LevelName = UpperSlabLevelName,
                        Name = "",
                        HostElementCode = "S1",
                    };
                    openings.Add(pumpOpening);
                }

                var screenOpening = new RectangularSlabOpeningDefinition
                {
                    Width = pl.B2,
                    Length = pl.B8,
                    Position = new Point2D(pr.B1 + pl.B2 / 2, pl.B8 / 2 + (pl.B8 + ts.T5) * i),

                    LevelName = UpperSlabLevelName,
                    Name = "",
                    HostElementCode = "S1",
                };
                openings.Add(screenOpening);
            }

            // 밸브실 상부 오프닝
            var valveRoomOpening = new RectangularSlabOpeningDefinition
            {
                Width = pr.B7,
                Length = d.N * pl.B8 + (d.N - 1) * ts.T5,
                Position = new Point2D(totalLength - ts.T4 - pr.B7 / 2, (totalWidth - ts.T4 * 2) / 2),
                LevelName = UpperSlabLevelName,
                Name = "",
                HostElementCode = "S1",
            };
            openings.Add(valveRoomOpening);

            return openings;
        }
        public static IReadOnlyList<CircularSlabOpeningDefinition> CalculateCircularSlabOpenings(PumpCreationRequestDto dto)
        {
            var d = dto.DesignConditionDto;
            var pr = dto.ProfileSpecDto;
            var pl = dto.PlanSpecDto;
            var ts = dto.TypeSelectionDto;
            var totalLength = pr.B1 + pl.B2 + pr.B3 + pr.B4 + pl.B5 + pr.B6 + ts.T3 + pr.B7 + ts.T4;
            var totalWidth = ts.T4 * 2 + (pl.B8 * d.N) + (ts.T5 * (d.N - 1));
            double x2 = totalLength - ts.T4 - pr.B7 - ts.T3 - pr.B6 - pl.B5 / 2 - pr.L4 - pr.L3;

            var openings = new List<CircularSlabOpeningDefinition>();


            if (!pl.IsRectangularOpening)
            {
                for (int i = 0; i < d.N; i++)
                {
                    var pumpOpening = new CircularSlabOpeningDefinition
                    {
                        Diameter = pl.B5,
                        Position = new Point2D(totalLength - ts.T4 - pr.B7 - ts.T3 - pr.B6 - pl.B5 / 2, pl.B8 / 2 + (pl.B8 + ts.T5) * i),

                        LevelName = UpperSlabLevelName,
                        Name = "",
                        HostElementCode = "S1",
                    };
                    openings.Add(pumpOpening);
                }
            }

            return openings;
        }
        public static IReadOnlyList<RectangularWallOpeningDefinition> CalculateRectangularWallOpenings(PumpCreationRequestDto dto)
        {
            var d = dto.DesignConditionDto;
            var pr = dto.ProfileSpecDto;
            var pl = dto.PlanSpecDto;
            var ts = dto.TypeSelectionDto;
            var totalLength = pr.B1 + pl.B2 + pr.B3 + pr.B4 + pl.B5 + pr.B6 + ts.T3 + pr.B7 + ts.T4;
            var totalWidth = ts.T4 * 2 + (pl.B8 * d.N) + (ts.T5 * (d.N - 1));
            var x2 = totalLength - ts.T4 - pr.B7 - ts.T3 - pr.B6 - pl.B5 / 2 - pr.L4 - pr.L3;

            var openings = new List<RectangularWallOpeningDefinition>();
            // 지 내벽 오프닝
            var innerWallOpening = new RectangularWallOpeningDefinition
            {
                Width = pr.OB1,
                Height = pr.OH1,
                Position = new Point3D(x2 + pr.L3 + pr.OB1 / 2, 0, 0),

                LevelName = FoundationPumpLevelName,
                Name = "",
                HostElementCode = "W3",
                OffsetZ = 0
            };
            openings.Add(innerWallOpening);

            // 지 사이벽 오프닝
            var partitionWall = new RectangularWallOpeningDefinition
            {
                Width = pr.OB1,
                Height = pr.OH1,
                Position = new Point3D(totalLength - ts.T4 - pl.L5 + pr.OB1 / 2, 0, 0),

                LevelName = FoundationPumpLevelName,
                Name = "",
                HostElementCode = "W5",
                OffsetZ = 0
            };
            openings.Add(partitionWall);

            return openings;
        }
        public static IReadOnlyList<CircularWallOpeningDefinition> CalculateCircularWallOpenings(PumpCreationRequestDto dto)
        {
            var d = dto.DesignConditionDto;
            var pr = dto.ProfileSpecDto;
            var pl = dto.PlanSpecDto;
            var ts = dto.TypeSelectionDto;
            var totalLength = pr.B1 + pl.B2 + pr.B3 + pr.B4 + pl.B5 + pr.B6 + ts.T3 + pr.B7 + ts.T4;
            var totalWidth = ts.T4 * 2 + (pl.B8 * d.N) + (ts.T5 * (d.N - 1));
            var x2 = totalLength - ts.T4 - pr.B7 - ts.T3 - pr.B6 - pl.B5 / 2 - pr.L4 - pr.L3;

            var openings = new List<CircularWallOpeningDefinition>();

            // 밸브실 외벽 오프닝
            for (int i = 0; i < d.N; i++)
            {
                var wallOpening = new CircularWallOpeningDefinition
                {
                    Diameter = d.D,
                    //Position = new Point3D(totalLength - ts.T4 / 2, pl.B8 / 2 + (pl.B8 + ts.T5) * i, 0),
                    Position = new Point3D(0, pl.B8 / 2 + (pl.B8 + ts.T5) * i, 0),

                    LevelName = ValveRoomLevelName,
                    Name = "",
                    HostElementCode = "W2",
                    OffsetZ = pr.H6
                };
                openings.Add(wallOpening);
            }
            // 밸브실 내벽 오프닝
            for (int i = 0; i < d.N; i++)
            {
                var wallOpening = new CircularWallOpeningDefinition
                {
                    Diameter = d.D,
                    //Position = new Point3D(totalLength - ts.T4 / 2, pl.B8 / 2 + (pl.B8 + ts.T5) * i, 0),
                    Position = new Point3D(0, pl.B8 / 2 + (pl.B8 + ts.T5) * i, 0),

                    LevelName = ValveRoomLevelName,
                    Name = "",
                    HostElementCode = "W4",
                    OffsetZ = pr.H6
                };
                openings.Add(wallOpening);
            }
            return openings;

        }
    }
}