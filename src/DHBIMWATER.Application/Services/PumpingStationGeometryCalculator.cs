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
            //var ts = dto.TypeSelectionDto;

            double upperSlab = d.HWL * 1000 + pr.H3 + pr.T1;

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
            //var ts = dto.TypeSelectionDto;
            var slabs = new List<SlabDefinition>();
            var totalLength = pr.B1 + pr.B2 + pr.B3 + pr.B4 + pr.B5 + pr.B6 + pr.T3 + pr.B7 + pr.T4;
            var totalWidth = pr.T4 * 2 + (pl.B8 * d.N) + (pl.T5 * (d.N - 1));

            var upperSlabDef = new SlabDefinition
            {
                Thickness = pr.T1,
                ElevationZ = d.HWL * 1000 + pr.H3 + pr.T1,
                LevelName = UpperSlabLevelName,
                ElementCode = "S1",
                Zone = "",
                Part = "상부슬래브",
            };
            var valveSlabDef = new SlabDefinition
            {
                Thickness = d.SelectedPumpingStationType == "Type2" ? pr.T3 : pr.T3,
                ElevationZ = upperSlabDef.ElevationZ - (pr.H7 + d.D + pr.H6),
                LevelName = ValveRoomLevelName,
                ElementCode = "MS1",
                Zone = "",
                Part = "밸브실슬래브",
            };
            // 상부슬래브 오프닝 추가
            upperSlabDef.SubPoints = new List<Point2D>()
            {
                new Point2D(totalLength - pr.T4 - pr.B7, 0),
                new Point2D(totalLength - pr.T4, 0),
                new Point2D(totalLength - pr.T4, totalWidth - pr.T4*2),
                new Point2D(totalLength - pr.T4 - pr.B7, totalWidth - pr.T4*2),
            };

            switch (d.SelectedEntranceType)
            {
                case "우안부":
                    upperSlabDef.Points = new List<Point2D>()
                    {
                        new Point2D(0, -pr.T4),
                        new Point2D(totalLength - (pr.T4 + pl.L5 + pr.T4 ) , -pr.T4),
                        new Point2D(totalLength - (pr.T4 + pl.L5 + pr.T4 ) , -pl.T5 - pl.B9 - pr.T4),
                        new Point2D(totalLength, -pl.T5 - pl.B9 - pr.T4),
                        new Point2D(totalLength, totalWidth - pr.T4),
                        new Point2D(0, totalWidth - pr.T4),
                    };

                    if (d.SelectedPumpingStationType == "Type1")
                    {
                        valveSlabDef.Points = new List<Point2D>()
                        {
                        new Point2D(totalLength - pr.T4 - pr.B7 - pr.T3, 0),
                        new Point2D(totalLength - pr.T4 , 0),
                        new Point2D(totalLength - pr.T4 , pl.B8 * d.N + pl.T5 * (d.N -1) ),
                        new Point2D(totalLength - pr.T4 - pr.B7 - pr.T3, pl.B8 * d.N + pl.T5 * (d.N -1) )
                        };
                    }
                    else if (d.SelectedPumpingStationType == "Type2")
                    {
                        valveSlabDef.Points = new List<Point2D>()
                        {
                        new Point2D(totalLength - pr.T4 - pr.B7, 0),
                        new Point2D(totalLength, 0),
                        new Point2D(totalLength, pl.B8 * d.N + pl.T5 * (d.N -1) + pr.T4),
                        new Point2D(totalLength - pr.T4 - pr.B7, pl.B8 * d.N + pl.T5 * (d.N -1)+ pr.T4 )
                        };
                    }
                    else
                    {
                        valveSlabDef.Points = new List<Point2D>()
                        {
                        new Point2D(totalLength - pr.T4 - pr.B7 , 0),
                        new Point2D(totalLength - pr.T4 , 0),
                        new Point2D(totalLength - pr.T4 , pl.B8 * d.N + pl.T5 * (d.N -1) ),
                        new Point2D(totalLength - pr.T4 - pr.B7 , pl.B8 * d.N + pl.T5 * (d.N -1) )
                        };
                    }

                    break;
                case "좌안부":
                    upperSlabDef.Points = new List<Point2D>()
                    {
                        new Point2D(0, -pr.T4),
                        new Point2D(totalLength, - pr.T4),
                        new Point2D(totalLength, totalWidth - pr.T4 + pl.T5 + pl.B9 ),
                        new Point2D(totalLength- (pr.T4 + pl.L5 + pr.T4 ), totalWidth - pr.T4 + pl.T5 + pl.B9 ),
                        new Point2D(totalLength- (pr.T4 + pl.L5 + pr.T4 ), totalWidth  - pr.T4),
                        new Point2D(0, totalWidth - pr.T4),
                    };

                    if (d.SelectedPumpingStationType == "Type1")
                    {
                        valveSlabDef.Points = new List<Point2D>()
                        {
                        new Point2D(totalLength - pr.T4 - pr.B7 - pr.T3, 0),
                        new Point2D(totalLength - pr.T4 , 0),
                        new Point2D(totalLength - pr.T4 , totalWidth - pr.T4*2 ),
                        new Point2D(totalLength - pr.T4 - pr.B7 - pr.T3, totalWidth - pr.T4*2 )
                        };
                    }
                    else if (d.SelectedPumpingStationType == "Type2")
                    {
                        valveSlabDef.Points = new List<Point2D>()
                        {
                        new Point2D(totalLength - pr.T4 - pr.B7 , -pr.T4),
                        new Point2D(totalLength , -pr.T4),
                        new Point2D(totalLength , pl.B8 * d.N + pl.T5 * (d.N -1) ),
                        new Point2D(totalLength - pr.T4 - pr.B7 , pl.B8 * d.N + pl.T5 * (d.N -1) )
                        };
                    }
                    else     // Type3
                    {
                        valveSlabDef.Points = new List<Point2D>()
                        {
                        new Point2D(totalLength - pr.T4 - pr.B7, 0),
                        new Point2D(totalLength - pr.T4 , 0),
                        new Point2D(totalLength - pr.T4 , totalWidth - pr.T4*2 ),
                        new Point2D(totalLength - pr.T4 - pr.B7, totalWidth - pr.T4*2 )
                        };
                    }
                    break;
                case "측면부":

                    if (d.SelectedPumpingStationType == "Type1")
                    {
                        upperSlabDef.Points = new List<Point2D>()
                    {
                        new Point2D(0, -pr.T4),
                        new Point2D(totalLength, - pr.T4),
                        new Point2D(totalLength, totalWidth- pr.T4),
                        new Point2D(0, totalWidth - pr.T4),
                    };
                        valveSlabDef.Points = new List<Point2D>()
                        {
                            new Point2D(totalLength - pr.T4 - pr.B7 - pr.T3, 0),
                            new Point2D(totalLength - pr.T4 , 0),
                            new Point2D(totalLength - pr.T4 , pl.B8 * d.N + pl.T5 * (d.N -1) ),
                            new Point2D(totalLength - pr.T4 - pr.B7 - pr.T3, pl.B8 * d.N + pl.T5 * (d.N -1) )
                        };
                    }
                    else if (d.SelectedPumpingStationType == "Type2")
                    {
                        upperSlabDef.Points = new List<Point2D>()
                    {
                        new Point2D(0, -pr.T4),
                        new Point2D(totalLength - pr.T4 + pr.T3, - pr.T4),
                        new Point2D(totalLength - pr.T4 + pr.T3, totalWidth- pr.T4),
                        new Point2D(0, totalWidth - pr.T4),
                    };
                        valveSlabDef.Points = new List<Point2D>()
                        {
                            new Point2D(totalLength - pr.T4 - pr.B7, -pr.T4),
                            new Point2D(totalLength- pr.T4 + pr.T3, -pr.T4),
                            new Point2D(totalLength- pr.T4 + pr.T3, pl.B8 * d.N + pl.T5 * (d.N -1)  +pr.T4),
                            new Point2D(totalLength - pr.T4 - pr.B7, pl.B8 * d.N + pl.T5 * (d.N -1)  + pr.T4)
                        };
                    }
                    else
                    {
                        upperSlabDef.Points = new List<Point2D>()
                    {
                        new Point2D(0, -pr.T4),
                        new Point2D(totalLength, - pr.T4),
                        new Point2D(totalLength, totalWidth- pr.T4),
                        new Point2D(0, totalWidth - pr.T4),
                    };
                        valveSlabDef.Points = new List<Point2D>()
                        {
                            new Point2D(totalLength - pr.T4 - pr.B7 , 0),
                            new Point2D(totalLength - pr.T4 , 0),
                            new Point2D(totalLength - pr.T4 , pl.B8 * d.N + pl.T5 * (d.N -1) ),
                            new Point2D(totalLength - pr.T4 - pr.B7 , pl.B8 * d.N + pl.T5 * (d.N -1) )
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
            //var ts = dto.TypeSelectionDto;
            var totalLength = pr.B1 + pr.B2 + pr.B3 + pr.B4 + pr.B5 + pr.B6 + pr.T3 + pr.B7 + pr.T4;
            var totalWidth = pr.T4 * 2 + (pl.B8 * d.N) + (pl.T5 * (d.N - 1));

            var linearWalls = new List<LinearWallDefinition>();

            // Linear 벽 계산 로직 추가 예정
            switch (d.SelectedEntranceType)
            {
                case "우안부":
                    // 우안부 외벽1 (우안부, Type 무관) - 짧은 외벽 - W1-2
                    var l_outerWallDef1 = new LinearWallDefinition
                    {
                        Thickness = pr.T4,
                        Height = pr.H5,
                        BaseOffset = 0,
                        LevelName = FoundationPumpLevelName,
                        ElementCode = "W1-2",
                        Zone = "펌프장",
                        Part = "펌프장 외벽",
                    };
                    l_outerWallDef1.StartPoint = new Point3D(totalLength - pr.T4 - pl.L5 - pr.T4 / 2, -pr.T4, 0);
                    l_outerWallDef1.EndPoint = new Point3D(totalLength - pr.T4 - pl.L5 - pr.T4 / 2, -pl.T5 - pl.B9 - pr.T4, 0);
                    l_outerWallDef1.IsFlipped = true;
                    linearWalls.Add(l_outerWallDef1);

                    // 우안부 외벽2 (우안부, Type 무관) - W1-3
                    var l_outerWallDef2 = new LinearWallDefinition
                    {
                        Thickness = pr.T4,
                        Height = pr.H5,
                        BaseOffset = 0,
                        LevelName = FoundationPumpLevelName,
                        ElementCode = "W1-3",
                        Zone = "펌프장",
                        Part = "펌프장 외벽",
                    };
                    l_outerWallDef2.StartPoint = new Point3D(totalLength - pr.T4 - pl.L5, -pl.T5 - pl.B9 - pr.T4 / 2, 0);
                    l_outerWallDef2.EndPoint = new Point3D(totalLength - pr.T4, -pl.T5 - pl.B9 - pr.T4 / 2, 0);
                    l_outerWallDef2.IsFlipped = true;
                    linearWalls.Add(l_outerWallDef2);

                    if (d.SelectedPumpingStationType == "Type1")
                    {
                        // 와류방지벽 (Type1 공통) - AVW
                        for (int i = 0; i < d.N; i++)
                        {
                            var antiVortexWallDef = new LinearWallDefinition
                            {
                                Thickness = pl.T6,
                                Height = pr.H5 + pr.T1 - pr.H7 - d.D - pr.H6 - pr.T3,
                                BaseOffset = 0,
                                LevelName = FoundationPumpLevelName,
                                ElementCode = "AVW",
                                Zone = "펌프장",
                                Part = "와류방지벽",
                            };

                            antiVortexWallDef.StartPoint = new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3, pl.B8 / 2 + (pl.B8 + pl.T5) * i, 0);
                            antiVortexWallDef.EndPoint = new Point3D(totalLength - pr.T4, pl.B8 / 2 + (pl.B8 + pl.T5) * i, 0);

                            linearWalls.Add(antiVortexWallDef);
                        }

                        // 밸브실 하부 내벽 (Type1 공통)
                        for (int i = 0; i < d.N - 1; i++)
                        {
                            var innerWallUnderValveDef = new LinearWallDefinition
                            {
                                Thickness = pl.T5,
                                Height = pr.H5 + pr.T1 - pr.H7 - d.D - pr.H6 - pr.T3,
                                BaseOffset = 0,
                                LevelName = FoundationPumpLevelName,
                                ElementCode = "W3-1",
                                Zone = "펌프장",
                                Part = "펌프장 내벽",
                            };

                            innerWallUnderValveDef.StartPoint = new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3, pl.B8 + pl.T5 / 2 + (pl.B8 + pl.T5) * i, 0);
                            innerWallUnderValveDef.EndPoint = new Point3D(totalLength - pr.T4, pl.B8 + pl.T5 / 2 + (pl.B8 + pl.T5) * i, 0);

                            linearWalls.Add(innerWallUnderValveDef);
                        }

                        // 우안부 내벽 (우안부, Type1, Type3 적용) - W5
                        var innerEntranceWallDef = new LinearWallDefinition
                        {
                            Thickness = pl.T5,
                            Height = pr.H5,
                            BaseOffset = 0,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "W5",
                            Zone = "펌프장",
                            Part = "펌프장 내벽",
                        };
                        innerEntranceWallDef.StartPoint = new Point3D(totalLength - pr.T4 - pl.L5, -pl.T5 / 2, 0);
                        innerEntranceWallDef.EndPoint = new Point3D(totalLength - pr.T4, -pl.T5 / 2, 0);
                        linearWalls.Add(innerEntranceWallDef);

                        // 우안부 외벽3 (우안부, Type1, Type3 적용) - 긴 외벽
                        var outerWallDef3 = new LinearWallDefinition
                        {
                            Thickness = pr.T4,
                            Height = pr.H5,
                            BaseOffset = 0,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "W2",
                            Zone = "펌프장",
                            Part = "펌프장 외벽",
                        };
                        outerWallDef3.StartPoint = new Point3D(totalLength - pr.T4 / 2, -pl.T5 - pl.B9 - pr.T4, 0);
                        outerWallDef3.EndPoint = new Point3D(totalLength - pr.T4 / 2, totalWidth - pr.T4, 0);
                        outerWallDef3.IsFlipped = true;
                        linearWalls.Add(outerWallDef3);

                        // 우안부 밸브실 사이벽 - W4
                        var valveRoomWallDef = new LinearWallDefinition
                        {
                            Thickness = pr.T3,
                            Height = pr.H7 + d.D + pr.H6 - pr.T1,
                            BaseOffset = 0,
                            LevelName = ValveRoomLevelName,
                            ElementCode = "W4",
                            Zone = "밸브실",
                            Part = "밸브실 사이벽",
                        };
                        valveRoomWallDef.StartPoint = new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3 / 2, 0, 0);
                        valveRoomWallDef.EndPoint = new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3 / 2, totalWidth - pr.T4 * 2, 0);
                        valveRoomWallDef.IsFlipped = true;
                        linearWalls.Add(valveRoomWallDef);
                    }
                    else if (d.SelectedPumpingStationType == "Type2")
                    {
                        // 우안부 Type2 벽체 계산 로직
                        // 우안부 내벽 (우안부, Type1, Type3 적용) - W5
                        var innerEntranceWallDef = new LinearWallDefinition
                        {
                            Thickness = pl.T5,
                            Height = pr.H5,
                            BaseOffset = 0,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "W5",
                            Zone = "펌프장",
                            Part = "펌프장 내벽",
                        };
                        innerEntranceWallDef.StartPoint = new Point3D(totalLength - pr.T4 - pl.L5, -pl.T5 / 2, 0);
                        innerEntranceWallDef.EndPoint = new Point3D(totalLength - pr.T4, -pl.T5 / 2, 0);
                        linearWalls.Add(innerEntranceWallDef);

                        // 우안부 외벽3 (우안부, Type2 적용) (동쪽) - W2
                        var outerWallDef3 = new LinearWallDefinition
                        {
                            Thickness = pr.T4,
                            Height = pr.H6 + d.D + pr.H7 - pr.T1,
                            BaseOffset = 0,
                            LevelName = ValveRoomLevelName,
                            ElementCode = "W2",
                            Zone = "펌프장",
                            Part = "펌프장 외벽",
                        };
                        outerWallDef3.StartPoint = new Point3D(totalLength - pr.T4 / 2, 0, 0);
                        outerWallDef3.EndPoint = new Point3D(totalLength - pr.T4 / 2, totalWidth - pr.T4, 0);
                        outerWallDef3.IsFlipped = true;
                        linearWalls.Add(outerWallDef3);

                        // 우안부 외벽3 (우안부, Type2 적용) (동쪽) - W2-1
                        var outerWallDef4 = new LinearWallDefinition
                        {
                            Thickness = pr.T4,
                            Height = pr.H5,
                            BaseOffset = 0,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "W2-1",
                            Zone = "펌프장",
                            Part = "펌프장 외벽",
                        };
                        outerWallDef4.StartPoint = new Point3D(totalLength - pr.T4 / 2, -pl.T5 - pl.B9 - pr.T4, 0);
                        outerWallDef4.EndPoint = new Point3D(totalLength - pr.T4 / 2, 0, 0);
                        outerWallDef4.IsFlipped = true;
                        linearWalls.Add(outerWallDef4);

                        // 우안부 밸브실 외벽3 (우안부, Type2 적용) (북쪽) - W1-5
                        var outerWallDef5 = new LinearWallDefinition
                        {
                            Thickness = pr.T4,
                            Height = pr.H6 + d.D + pr.H7 - pr.T1,
                            BaseOffset = 0,
                            LevelName = ValveRoomLevelName,
                            ElementCode = "W1-5",
                            Zone = "펌프장",
                            Part = "펌프장 외벽",
                        };
                        outerWallDef5.StartPoint = new Point3D(totalLength - pr.T4 - pr.B7, totalWidth - pr.T4 * 3 / 2, 0);
                        outerWallDef5.EndPoint = new Point3D(totalLength - pr.T4, totalWidth - pr.T4 * 3 / 2, 0);
                        outerWallDef5.IsFlipped = true;
                        linearWalls.Add(outerWallDef5);

                        // 우안부 밸브실 사이벽 - W4
                        var valveRoomWallDef = new LinearWallDefinition
                        {
                            Thickness = pr.T3,
                            Height = pr.H5,
                            BaseOffset = 0,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "W4",
                            Zone = "밸브실",
                            Part = "밸브실 사이벽",
                        };
                        valveRoomWallDef.StartPoint = new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3 / 2, 0, 0);
                        valveRoomWallDef.EndPoint = new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3 / 2, totalWidth - pr.T4, 0);
                        valveRoomWallDef.IsFlipped = true;
                        linearWalls.Add(valveRoomWallDef);
                    }
                    else if (d.SelectedPumpingStationType == "Type3")
                    {
                        // 좌안부 Type3 벽체 계산 로직
                        // 좌안부 내벽 (좌안부, Type1, Type3 적용)
                        var innerEntranceWallDef = new LinearWallDefinition
                        {
                            Thickness = pl.T5,
                            Height = pr.H5,
                            BaseOffset = 0,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "W5",
                            Zone = "펌프장",
                            Part = "펌프장 내벽",
                        };
                        innerEntranceWallDef.StartPoint = new Point3D(totalLength - pr.T4 - pl.L5, -pl.T5 / 2, 0);
                        innerEntranceWallDef.EndPoint = new Point3D(totalLength - pr.T4, -pl.T5 / 2, 0);
                        linearWalls.Add(innerEntranceWallDef);

                        // 좌안부 외벽3 (좌안부, Type1, Type3 적용) - 긴 외벽
                        var outerWallDef3 = new LinearWallDefinition
                        {
                            Thickness = pr.T4,
                            Height = pr.H5,
                            BaseOffset = 0,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "W2",
                            Zone = "펌프장",
                            Part = "펌프장 외벽",
                        };
                        outerWallDef3.StartPoint = new Point3D(totalLength - pr.T4 / 2, -pl.T5 - pl.B9 - pr.T4, 0);
                        outerWallDef3.EndPoint = new Point3D(totalLength - pr.T4 / 2, totalWidth - pr.T4, 0);
                        outerWallDef3.IsFlipped = true;
                        linearWalls.Add(outerWallDef3);

                        // 좌안부 밸브실 사이벽 - W4
                        var valveRoomWallDef = new LinearWallDefinition
                        {
                            Thickness = pr.T3,
                            Height = pr.H5,
                            BaseOffset = 0,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "W4",
                            Zone = "밸브실",
                            Part = "밸브실 사이벽",
                        };
                        valveRoomWallDef.StartPoint = new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3 / 2, 0, 0);
                        valveRoomWallDef.EndPoint = new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3 / 2, totalWidth - pr.T4 * 2, 0);
                        valveRoomWallDef.IsFlipped = true;
                        linearWalls.Add(valveRoomWallDef);
                    }
                    break;
                case "좌안부":
                    // 좌안부 외벽1 (좌안부, Type 무관) - 짧은 외벽 - W1-2
                    var r_outerWallDef1 = new LinearWallDefinition
                    {
                        Thickness = pr.T4,
                        Height = pr.H5,
                        BaseOffset = 0,
                        LevelName = FoundationPumpLevelName,
                        ElementCode = "W1-2",
                        Zone = "펌프장",
                        Part = "펌프장 외벽",
                    };
                    r_outerWallDef1.StartPoint = new Point3D(totalLength - pr.T4 - pl.L5 - pr.T4 / 2, totalWidth - pr.T4 * 2 - (-pr.T4), 0);
                    r_outerWallDef1.EndPoint = new Point3D(totalLength - pr.T4 - pl.L5 - pr.T4 / 2, totalWidth - pr.T4 * 2 - (-pl.T5 - pl.B9 - pr.T4), 0);
                    r_outerWallDef1.IsFlipped = true;
                    linearWalls.Add(r_outerWallDef1);

                    // 좌안부 외벽2 (좌안부, Type 무관) - W1-3
                    var r_outerWallDef2 = new LinearWallDefinition
                    {
                        Thickness = pr.T4,
                        Height = pr.H5,
                        BaseOffset = 0,
                        LevelName = FoundationPumpLevelName,
                        ElementCode = "W1-3",
                        Zone = "펌프장",
                        Part = "펌프장 외벽",
                    };
                    r_outerWallDef2.StartPoint = new Point3D(totalLength - pr.T4 - pl.L5, totalWidth - pr.T4 * 2 - (-pl.T5 - pl.B9 - pr.T4 / 2), 0);
                    r_outerWallDef2.EndPoint = new Point3D(totalLength - pr.T4, totalWidth - pr.T4 * 2 - (-pl.T5 - pl.B9 - pr.T4 / 2), 0);
                    r_outerWallDef2.IsFlipped = true;
                    linearWalls.Add(r_outerWallDef2);

                    if (d.SelectedPumpingStationType == "Type1")
                    {
                        // 와류방지벽 (Type1 공통) - AVW
                        for (int i = 0; i < d.N; i++)
                        {
                            var antiVortexWallDef = new LinearWallDefinition
                            {
                                Thickness = pl.T6,
                                Height = pr.H5 + pr.T1 - pr.H7 - d.D - pr.H6 - pr.T3,
                                BaseOffset = 0,
                                LevelName = FoundationPumpLevelName,
                                ElementCode = "AVW",
                                Zone = "펌프장",
                                Part = "와류방지벽",
                            };

                            antiVortexWallDef.StartPoint = new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3, totalWidth - pr.T4 * 2 - (pl.B8 / 2 + (pl.B8 + pl.T5) * i), 0);
                            antiVortexWallDef.EndPoint = new Point3D(totalLength - pr.T4, totalWidth - pr.T4 * 2 - (pl.B8 / 2 + (pl.B8 + pl.T5) * i), 0);

                            linearWalls.Add(antiVortexWallDef);
                        }

                        // 밸브실 하부 내벽 (Type1 공통)
                        for (int i = 0; i < d.N - 1; i++)
                        {
                            var innerWallUnderValveDef = new LinearWallDefinition
                            {
                                Thickness = pl.T5,
                                Height = pr.H5 + pr.T1 - pr.H7 - d.D - pr.H6 - pr.T3,
                                BaseOffset = 0,
                                LevelName = FoundationPumpLevelName,
                                ElementCode = "W3-1",
                                Zone = "펌프장",
                                Part = "펌프장 내벽",
                            };

                            innerWallUnderValveDef.StartPoint = new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3, totalWidth - pr.T4 * 2 - (pl.B8 + pl.T5 / 2 + (pl.B8 + pl.T5) * i), 0);
                            innerWallUnderValveDef.EndPoint = new Point3D(totalLength - pr.T4, totalWidth - pr.T4 * 2 - (pl.B8 + pl.T5 / 2 + (pl.B8 + pl.T5) * i), 0);

                            linearWalls.Add(innerWallUnderValveDef);
                        }

                        // 좌안부 내벽 (좌안부, Type1, Type3 적용) - W5
                        var innerEntranceWallDef = new LinearWallDefinition
                        {
                            Thickness = pl.T5,
                            Height = pr.H5,
                            BaseOffset = 0,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "W5",
                            Zone = "펌프장",
                            Part = "펌프장 내벽",
                        };
                        innerEntranceWallDef.StartPoint = new Point3D(totalLength - pr.T4 - pl.L5, totalWidth - pr.T4 * 2 - (-pl.T5 / 2), 0);
                        innerEntranceWallDef.EndPoint = new Point3D(totalLength - pr.T4, totalWidth - pr.T4 * 2 - (-pl.T5 / 2), 0);
                        linearWalls.Add(innerEntranceWallDef);

                        // 좌안부 외벽3 (좌안부, Type1, Type3 적용) - 긴 외벽
                        var outerWallDef3 = new LinearWallDefinition
                        {
                            Thickness = pr.T4,
                            Height = pr.H5,
                            BaseOffset = 0,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "W2",
                            Zone = "펌프장",
                            Part = "펌프장 외벽",
                        };
                        outerWallDef3.StartPoint = new Point3D(totalLength - pr.T4 / 2, totalWidth - pr.T4 * 2 - (-pl.T5 - pl.B9 - pr.T4), 0);
                        outerWallDef3.EndPoint = new Point3D(totalLength - pr.T4 / 2, totalWidth - pr.T4 * 2 - (totalWidth - pr.T4), 0);
                        outerWallDef3.IsFlipped = true;
                        linearWalls.Add(outerWallDef3);

                        // 좌안부 밸브실 사이벽 - W4
                        var valveRoomWallDef = new LinearWallDefinition
                        {
                            Thickness = pr.T3,
                            Height = pr.H7 + d.D + pr.H6 - pr.T1,
                            BaseOffset = 0,
                            LevelName = ValveRoomLevelName,
                            ElementCode = "W4",
                            Zone = "밸브실",
                            Part = "밸브실 사이벽",
                        };
                        valveRoomWallDef.StartPoint = new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3 / 2, totalWidth - pr.T4 * 2 - (0), 0);
                        valveRoomWallDef.EndPoint = new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3 / 2, totalWidth - pr.T4 * 2 - (totalWidth - pr.T4 * 2), 0);
                        valveRoomWallDef.IsFlipped = true;
                        linearWalls.Add(valveRoomWallDef);
                    }
                    else if (d.SelectedPumpingStationType == "Type2")
                    {
                        // 좌안부 Type2 벽체 계산 로직
                        // 좌안부 내벽 (좌안부, Type1, Type3 적용) - W5
                        var innerEntranceWallDef = new LinearWallDefinition
                        {
                            Thickness = pl.T5,
                            Height = pr.H5,
                            BaseOffset = 0,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "W5",
                            Zone = "펌프장",
                            Part = "펌프장 내벽",
                        };
                        innerEntranceWallDef.StartPoint = new Point3D(totalLength - pr.T4 - pl.L5, totalWidth - pr.T4 * 2 - (-pl.T5 / 2), 0);
                        innerEntranceWallDef.EndPoint = new Point3D(totalLength - pr.T4, totalWidth - pr.T4 * 2 - (-pl.T5 / 2), 0);
                        linearWalls.Add(innerEntranceWallDef);

                        // 좌안부 외벽3 (좌안부, Type2 적용) (동쪽) - W2
                        var outerWallDef3 = new LinearWallDefinition
                        {
                            Thickness = pr.T4,
                            Height = pr.H6 + d.D + pr.H7 - pr.T1,
                            BaseOffset = 0,
                            LevelName = ValveRoomLevelName,
                            ElementCode = "W2",
                            Zone = "펌프장",
                            Part = "펌프장 외벽",
                        };
                        outerWallDef3.StartPoint = new Point3D(totalLength - pr.T4 / 2, totalWidth - pr.T4 * 2 - (0), 0);
                        outerWallDef3.EndPoint = new Point3D(totalLength - pr.T4 / 2, totalWidth - pr.T4 * 2 - (totalWidth - pr.T4), 0);
                        outerWallDef3.IsFlipped = true;
                        linearWalls.Add(outerWallDef3);

                        // 좌안부 외벽3 (좌안부, Type2 적용) (동쪽) - W2-1
                        var outerWallDef4 = new LinearWallDefinition
                        {
                            Thickness = pr.T4,
                            Height = pr.H5,
                            BaseOffset = 0,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "W2-1",
                            Zone = "펌프장",
                            Part = "펌프장 외벽",
                        };
                        outerWallDef4.StartPoint = new Point3D(totalLength - pr.T4 / 2, totalWidth - pr.T4 * 2 - (-pl.T5 - pl.B9 - pr.T4), 0);
                        outerWallDef4.EndPoint = new Point3D(totalLength - pr.T4 / 2, totalWidth - pr.T4 * 2 - (0), 0);
                        outerWallDef4.IsFlipped = true;
                        linearWalls.Add(outerWallDef4);

                        // 좌안부 밸브실 외벽3 (좌안부, Type2 적용) (북쪽) - W1-5
                        var outerWallDef5 = new LinearWallDefinition
                        {
                            Thickness = pr.T4,
                            Height = pr.H6 + d.D + pr.H7 - pr.T1,
                            BaseOffset = 0,
                            LevelName = ValveRoomLevelName,
                            ElementCode = "W1-5",
                            Zone = "펌프장",
                            Part = "펌프장 외벽",
                        };
                        outerWallDef5.StartPoint = new Point3D(totalLength - pr.T4 - pr.B7, totalWidth - pr.T4 * 2 - (totalWidth - pr.T4 * 3 / 2), 0);
                        outerWallDef5.EndPoint = new Point3D(totalLength - pr.T4, totalWidth - pr.T4 * 2 - (totalWidth - pr.T4 * 3 / 2), 0);
                        outerWallDef5.IsFlipped = true;
                        linearWalls.Add(outerWallDef5);

                        // 좌안부 밸브실 사이벽 - W4
                        var valveRoomWallDef = new LinearWallDefinition
                        {
                            Thickness = pr.T3,
                            Height = pr.H5,
                            BaseOffset = 0,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "W4",
                            Zone = "밸브실",
                            Part = "밸브실 사이벽",
                        };
                        valveRoomWallDef.StartPoint = new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3 / 2, totalWidth - pr.T4 * 2 - (0), 0);
                        valveRoomWallDef.EndPoint = new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3 / 2, totalWidth - pr.T4 * 2 - (totalWidth - pr.T4), 0);
                        valveRoomWallDef.IsFlipped = true;
                        linearWalls.Add(valveRoomWallDef);
                    }
                    else if (d.SelectedPumpingStationType == "Type3")
                    {
                        // 좌안부 Type3 벽체 계산 로직
                        // 좌안부 내벽 (좌안부, Type1, Type3 적용)
                        var innerEntranceWallDef = new LinearWallDefinition
                        {
                            Thickness = pl.T5,
                            Height = pr.H5,
                            BaseOffset = 0,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "W5",
                            Zone = "펌프장",
                            Part = "펌프장 내벽",
                        };
                        innerEntranceWallDef.StartPoint = new Point3D(totalLength - pr.T4 - pl.L5, totalWidth - pr.T4 * 2 - (-pl.T5 / 2), 0);
                        innerEntranceWallDef.EndPoint = new Point3D(totalLength - pr.T4, totalWidth - pr.T4 * 2 - (-pl.T5 / 2), 0);
                        linearWalls.Add(innerEntranceWallDef);

                        // 좌안부 외벽3 (좌안부, Type1, Type3 적용) - 긴 외벽
                        var outerWallDef3 = new LinearWallDefinition
                        {
                            Thickness = pr.T4,
                            Height = pr.H5,
                            BaseOffset = 0,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "W2",
                            Zone = "펌프장",
                            Part = "펌프장 외벽",
                        };
                        outerWallDef3.StartPoint = new Point3D(totalLength - pr.T4 / 2, totalWidth - pr.T4 * 2 - (-pl.T5 - pl.B9 - pr.T4), 0);
                        outerWallDef3.EndPoint = new Point3D(totalLength - pr.T4 / 2, totalWidth - pr.T4 * 2 - (totalWidth - pr.T4), 0);
                        outerWallDef3.IsFlipped = true;
                        linearWalls.Add(outerWallDef3);

                        // 좌안부 밸브실 사이벽 - W4
                        var valveRoomWallDef = new LinearWallDefinition
                        {
                            Thickness = pr.T3,
                            Height = pr.H5,
                            BaseOffset = 0,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "W4",
                            Zone = "밸브실",
                            Part = "밸브실 사이벽",
                        };
                        valveRoomWallDef.StartPoint = new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3 / 2, totalWidth - pr.T4 * 2 - (0), 0);
                        valveRoomWallDef.EndPoint = new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3 / 2, totalWidth - pr.T4 * 2 - (totalWidth - pr.T4 * 2), 0);
                        valveRoomWallDef.IsFlipped = true;
                        linearWalls.Add(valveRoomWallDef);
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
                                Thickness = pl.T6,
                                Height = pr.H5 + pr.T1 - pr.H7 - d.D - pr.H6 - pr.T3,
                                BaseOffset = 0,
                                LevelName = FoundationPumpLevelName,
                                ElementCode = "AVW",
                                Zone = "펌프장",
                                Part = "와류방지벽",
                            };

                            antiVortexWallDef.StartPoint = new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3, pl.B8 / 2 + (pl.B8 + pl.T5) * i, 0);
                            antiVortexWallDef.EndPoint = new Point3D(totalLength - pr.T4, pl.B8 / 2 + (pl.B8 + pl.T5) * i, 0);

                            linearWalls.Add(antiVortexWallDef);
                        }

                        // 밸브실 하부 내벽 (Type1 공통)
                        for (int i = 0; i < d.N - 1; i++)
                        {
                            var innerWallUnderValveDef = new LinearWallDefinition
                            {
                                Thickness = pl.T5,
                                Height = pr.H5 + pr.T1 - pr.H7 - d.D - pr.H6 - pr.T3,
                                BaseOffset = 0,
                                LevelName = FoundationPumpLevelName,
                                ElementCode = "W3-1",
                                Zone = "펌프장",
                                Part = "펌프장 내벽",
                            };

                            innerWallUnderValveDef.StartPoint = new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3, pl.B8 + pl.T5 / 2 + (pl.B8 + pl.T5) * i, 0);
                            innerWallUnderValveDef.EndPoint = new Point3D(totalLength - pr.T4, pl.B8 + pl.T5 / 2 + (pl.B8 + pl.T5) * i, 0);

                            linearWalls.Add(innerWallUnderValveDef);
                        }

                        // 외벽3 - 동쪽 - W2
                        var outerWallDef3 = new LinearWallDefinition
                        {
                            Thickness = pr.T4,
                            Height = pr.H5,
                            BaseOffset = 0,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "W2",
                            Zone = "펌프장",
                            Part = "펌프장 외벽",
                        };
                        outerWallDef3.StartPoint = new Point3D(totalLength - pr.T4 / 2, -pr.T4, 0);
                        outerWallDef3.EndPoint = new Point3D(totalLength - pr.T4 / 2, totalWidth - pr.T4, 0);
                        outerWallDef3.IsFlipped = true;
                        linearWalls.Add(outerWallDef3);

                        // 좌안부 밸브실 사이벽 - W4
                        var valveRoomWallDef = new LinearWallDefinition
                        {
                            Thickness = pr.T3,
                            Height = pr.H7 + d.D + pr.H6 - pr.T1,
                            BaseOffset = 0,
                            LevelName = ValveRoomLevelName,
                            ElementCode = "W4",
                            Zone = "밸브실",
                            Part = "밸브실 사이벽",
                        };
                        valveRoomWallDef.StartPoint = new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3 / 2, 0, 0);
                        valveRoomWallDef.EndPoint = new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3 / 2, totalWidth - pr.T4 * 2, 0);
                        valveRoomWallDef.IsFlipped = true;
                        linearWalls.Add(valveRoomWallDef);
                    }
                    else if (d.SelectedPumpingStationType == "Type2")
                    {
                        // 측면부 Type2 벽체 계산 로직
                        // 측면부 밸브실 사이벽 - W4
                        var valveRoomWallDef = new LinearWallDefinition
                        {
                            Thickness = pr.T3,
                            Height = pr.T3 + pr.H6 + d.D + pr.H7 - pr.T1,
                            BaseOffset = -pr.T3,
                            LevelName = ValveRoomLevelName,
                            ElementCode = "W4",
                            Zone = "밸브실",
                            Part = "밸브실 사이벽",
                        };
                        valveRoomWallDef.StartPoint = new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3 / 2, -pr.T4, 0);
                        valveRoomWallDef.EndPoint = new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3 / 2, totalWidth - pr.T4, 0);
                        valveRoomWallDef.IsFlipped = true;
                        linearWalls.Add(valveRoomWallDef);

                        // 측면부 밸브실 사이벽 - W4-1
                        var valveRoomWallDef2 = new LinearWallDefinition
                        {
                            Thickness = pr.T4,
                            Height = pr.H5 + pr.T1 - pr.H7 - d.D - pr.H6 - pr.T3,
                            BaseOffset = 0,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "W4-1",
                            Zone = "밸브실",
                            Part = "밸브실 하부 외벽",
                        };
                        valveRoomWallDef2.StartPoint = new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3 + pr.T4 / 2, -pr.T4, 0);
                        valveRoomWallDef2.EndPoint = new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3 + pr.T4 / 2, totalWidth - pr.T4, 0);
                        valveRoomWallDef2.IsFlipped = true;
                        linearWalls.Add(valveRoomWallDef2);


                        // 외벽2 - 동쪽 - W2
                        var outerWallDef2 = new LinearWallDefinition
                        {
                            Thickness = pr.T3,
                            Height = pr.H7 + d.D + pr.H6 - pr.T1,
                            BaseOffset = 0,
                            LevelName = ValveRoomLevelName,
                            ElementCode = "W2",
                            Zone = "펌프장",
                            Part = "펌프장 외벽",
                        };
                        outerWallDef2.StartPoint = new Point3D(totalLength - pr.T4 + pr.T3 / 2, -pr.T4, 0);
                        outerWallDef2.EndPoint = new Point3D(totalLength - pr.T4 + pr.T3 / 2, totalWidth - pr.T4, 0);
                        outerWallDef2.IsFlipped = true;
                        linearWalls.Add(outerWallDef2);

                        // 외벽1 - 남쪽 - W1-1
                        var outerWallDef1 = new LinearWallDefinition
                        {
                            Thickness = pr.T4,
                            Height = pr.H7 + d.D + pr.H6 - pr.T1,
                            BaseOffset = 0,
                            LevelName = ValveRoomLevelName,
                            ElementCode = "W1-1",
                            Zone = "펌프장",
                            Part = "펌프장 외벽",
                        };
                        outerWallDef1.StartPoint = new Point3D(totalLength - pr.T4 - pr.B7, -pr.T4 / 2, 0);
                        outerWallDef1.EndPoint = new Point3D(totalLength - pr.T4, -pr.T4 / 2, 0);
                        outerWallDef1.IsFlipped = true;
                        linearWalls.Add(outerWallDef1);

                        // 외벽3 - 북쪽 - W1-1
                        var outerWallDef3 = new LinearWallDefinition
                        {
                            Thickness = pr.T4,
                            Height = pr.H7 + d.D + pr.H6 - pr.T1,
                            BaseOffset = 0,
                            LevelName = ValveRoomLevelName,
                            ElementCode = "W1-1",
                            Zone = "펌프장",
                            Part = "펌프장 외벽",
                        };
                        outerWallDef3.StartPoint = new Point3D(totalLength - pr.T4 - pr.B7, totalWidth - pr.T4 - pr.T4 / 2, 0);
                        outerWallDef3.EndPoint = new Point3D(totalLength - pr.T4, totalWidth - pr.T4 - pr.T4 / 2, 0);
                        outerWallDef3.IsFlipped = true;
                        linearWalls.Add(outerWallDef3);

                    }
                    else if (d.SelectedPumpingStationType == "Type3")
                    {
                        // 측면부 Type3 벽체 계산 로직
                        // 좌안부 밸브실 사이벽 - W4
                        var valveRoomWallDef = new LinearWallDefinition
                        {
                            Thickness = pr.T3,
                            Height = pr.T3 + pr.H6 + d.D + pr.H7 - pr.T1,
                            BaseOffset = -pr.T3,
                            LevelName = ValveRoomLevelName,
                            ElementCode = "W4",
                            Zone = "밸브실",
                            Part = "밸브실 사이벽",
                        };
                        valveRoomWallDef.StartPoint = new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3 / 2, 0, 0);
                        valveRoomWallDef.EndPoint = new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3 / 2, totalWidth - pr.T4 * 2, 0);
                        valveRoomWallDef.IsFlipped = true;
                        linearWalls.Add(valveRoomWallDef);

                        // 좌안부 밸브실 사이벽 - W4-1
                        var valveRoomWallDef2 = new LinearWallDefinition
                        {
                            Thickness = pr.T4,
                            Height = pr.H5 + pr.T1 - pr.H7 - d.D - pr.H6 - pr.T3,
                            BaseOffset = 0,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "W4-1",
                            Zone = "밸브실",
                            Part = "밸브실 하부벽",
                        };
                        valveRoomWallDef2.StartPoint = new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3  + pr.T4 / 2, 0, 0);
                        valveRoomWallDef2.EndPoint = new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3  + pr.T4 / 2, totalWidth - pr.T4 * 2, 0);
                        valveRoomWallDef2.IsFlipped = true;
                        linearWalls.Add(valveRoomWallDef2);

                        // 외벽3 - 동쪽 - W2
                        var outerWallDef3 = new LinearWallDefinition
                        {
                            Thickness = pr.T4,
                            Height = pr.H5,
                            BaseOffset = 0,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "W2",
                            Zone = "펌프장",
                            Part = "펌프장 외벽",
                        };
                        outerWallDef3.StartPoint = new Point3D(totalLength - pr.T4 / 2, -pr.T4, 0);
                        outerWallDef3.EndPoint = new Point3D(totalLength - pr.T4 / 2, totalWidth - pr.T4, 0);
                        outerWallDef3.IsFlipped = true;
                        linearWalls.Add(outerWallDef3);
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
            //var ts = dto.TypeSelectionDto;
            var totalLength = pr.B1 + pr.B2 + pr.B3 + pr.B4 + pr.B5 + pr.B6 + pr.T3 + pr.B7 + pr.T4;
            var totalWidth = pr.T4 * 2 + (pl.B8 * d.N) + (pl.T5 * (d.N - 1));
            double x2 = totalLength - pr.T4 - pr.B7 - pr.T3 - pr.B6 - pr.B5 / 2 - pr.L4 - pr.L3;

            var profileWalls = new List<ProfileWallDefinition>();

            // 지 사이 내벽 (공통) - W3
            for (int i = 0; i < d.N - 1; i++)
            {
                var innerProfileWallDef = new ProfileWallDefinition
                {
                    Thickness = pl.T5,
                    LevelName = FoundationPumpLevelName,
                    ElementCode = "W3",
                    Zone = "",
                    Part = ""
                };
                innerProfileWallDef.Points = new List<Point3D>() {
                            new Point3D(0, -pl.T5/2 + (pl.B8 + pl.T5)*(i+1), d.LWL * 1000 - pr.H1),
                            new Point3D(x2, -pl.T5/2 + (pl.B8 + pl.T5)*(i+1), d.LWL * 1000 - pr.H1),
                            new Point3D(x2 + pr.L3, -pl.T5/2 + (pl.B8 + pl.T5)*(i+1),  d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3, -pl.T5/2 + (pl.B8 + pl.T5)*(i+1), d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3, -pl.T5/2 + (pl.B8 + pl.T5)*(i+1), d.HWL * 1000 + pr.H3),
                            new Point3D(0, -pl.T5/2 + (pl.B8 + pl.T5)*(i+1), d.HWL * 1000 + pr.H3),
                        };
                profileWalls.Add(innerProfileWallDef);
            }

            // Profile 벽 계산 로직 추가 예정
            switch (d.SelectedEntranceType)
            {
                case "우안부":
                    // 우안부 공통 - 진입부측 프로파일 (짧은 벽체) - W1-1
                    var l_outerProfileWallDef1 = new ProfileWallDefinition
                    {
                        Thickness = pr.T4,
                        LevelName = FoundationPumpLevelName,
                        ElementCode = "W1-1",
                        Zone = "",
                        Part = ""
                    };
                    l_outerProfileWallDef1.Points = new List<Point3D>() {
                            new Point3D(0, -pr.T4/2, d.LWL * 1000 - pr.H1),
                            new Point3D(x2, -pr.T4/2, d.LWL * 1000 - pr.H1),
                            new Point3D(x2 + pr.L3, -pr.T4/2,  d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - pr.T4 - pl.L5, -pr.T4/2, d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - pr.T4 - pl.L5, -pr.T4/2, d.HWL * 1000 + pr.H3),
                            new Point3D(0, -pr.T4/2, d.HWL * 1000 + pr.H3),
                        };
                    profileWalls.Add(l_outerProfileWallDef1);

                    if (d.SelectedPumpingStationType == "Type1")
                    {
                        // 우안부 외벽 - 진입부 반대측 프로파일 (긴 벽체, Type1, Type3) - W1
                        var l_outerProfileWallDef2 = new ProfileWallDefinition
                        {
                            Thickness = pr.T4,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "W1",
                            Zone = "",
                            Part = ""
                        };
                        l_outerProfileWallDef2.Points = new List<Point3D>() {
                            new Point3D(0, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2, d.LWL * 1000 - pr.H1),
                            new Point3D(x2, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2, d.LWL * 1000 - pr.H1),
                            new Point3D(x2 + pr.L3, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2,  d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - pr.T4, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2, d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - pr.T4, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2, d.HWL * 1000 + pr.H3),
                            new Point3D(0, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2, d.HWL * 1000 + pr.H3),
                        };
                        profileWalls.Add(l_outerProfileWallDef2);
                    }
                    else if (d.SelectedPumpingStationType == "Type2")
                    {
                        // 우안부 Type2 벽체 계산 로직
                        // 우안부 외벽 - 진입부 반대측 프로파일 (긴 벽체, Type2) - W1-4
                        var l_outerProfileWallDef2 = new ProfileWallDefinition
                        {
                            Thickness = pr.T4,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "W1-4",
                            Zone = "",
                            Part = ""
                        };
                        l_outerProfileWallDef2.Points = new List<Point3D>() {
                            new Point3D(0, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2, d.LWL * 1000 - pr.H1),
                            new Point3D(x2, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2, d.LWL * 1000 - pr.H1),
                            new Point3D(x2 + pr.L3, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2,  d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2, d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2, d.HWL * 1000 + pr.H3),
                            new Point3D(0, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2, d.HWL * 1000 + pr.H3),
                        };
                        profileWalls.Add(l_outerProfileWallDef2);
                    }
                    else if (d.SelectedPumpingStationType == "Type3")
                    {
                        // 우안부 Type3 벽체 계산 로직
                        // 우안부 외벽 - 진입부 반대측 프로파일 (긴 벽체, Type1, Type3) - W1
                        var l_outerProfileWallDef2 = new ProfileWallDefinition
                        {
                            Thickness = pr.T4,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "W1",
                            Zone = "",
                            Part = ""
                        };
                        l_outerProfileWallDef2.Points = new List<Point3D>() {
                            new Point3D(0, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2, d.LWL * 1000 - pr.H1),
                            new Point3D(x2, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2, d.LWL * 1000 - pr.H1),
                            new Point3D(x2 + pr.L3, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2,  d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - pr.T4, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2, d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - pr.T4, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2, d.HWL * 1000 + pr.H3),
                            new Point3D(0, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2, d.HWL * 1000 + pr.H3),
                        };
                        profileWalls.Add(l_outerProfileWallDef2);
                    }
                    break;
                case "좌안부":
                    // 좌안부 공통 - 진입부측 프로파일 (짧은 벽체) - W1-1
                    var r_outerProfileWallDef1 = new ProfileWallDefinition
                    {
                        Thickness = pr.T4,
                        LevelName = FoundationPumpLevelName,
                        ElementCode = "W1-1",
                        Zone = "",
                        Part = ""
                    };
                    r_outerProfileWallDef1.Points = new List<Point3D>() {
                            new Point3D(0, totalWidth - pr.T4 - pr.T4/2, d.LWL * 1000 - pr.H1),
                            new Point3D(x2, totalWidth - pr.T4 - pr.T4/2, d.LWL * 1000 - pr.H1),
                            new Point3D(x2 + pr.L3, totalWidth - pr.T4 - pr.T4/2,  d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - pr.T4 - pl.L5, totalWidth - pr.T4 - pr.T4/2, d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - pr.T4 - pl.L5, totalWidth - pr.T4 - pr.T4/2, d.HWL * 1000 + pr.H3),
                            new Point3D(0, totalWidth - pr.T4 - pr.T4/2, d.HWL * 1000 + pr.H3),
                        };
                    profileWalls.Add(r_outerProfileWallDef1);

                    if (d.SelectedPumpingStationType == "Type1")
                    {
                        // 좌안부 Type1 벽체 계산 로직
                        // 좌안부 외벽 - 진입부 반대측 프로파일 (긴 벽체, Type1, Type3) - W1
                        var r_outerProfileWallDef2 = new ProfileWallDefinition
                        {
                            Thickness = pr.T4,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "W1",
                            Zone = "",
                            Part = ""
                        };
                        r_outerProfileWallDef2.Points = new List<Point3D>() {
                            new Point3D(0, -pr.T4/2, d.LWL * 1000 - pr.H1),
                            new Point3D(x2, -pr.T4/2, d.LWL * 1000 - pr.H1),
                            new Point3D(x2 + pr.L3, -pr.T4/2,  d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - pr.T4, -pr.T4/2, d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - pr.T4, -pr.T4/2, d.HWL * 1000 + pr.H3),
                            new Point3D(0, -pr.T4/2, d.HWL * 1000 + pr.H3),
                        };
                        profileWalls.Add(r_outerProfileWallDef2);
                    }
                    else if (d.SelectedPumpingStationType == "Type2")
                    {
                        // 좌안부 Type2 벽체 계산 로직
                        // 좌안부 외벽 - 진입부 반대측 프로파일 (긴 벽체, Type2) - W1-4
                        var r_outerProfileWallDef2 = new ProfileWallDefinition
                        {
                            Thickness = pr.T4,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "W1-4",
                            Zone = "",
                            Part = ""
                        };
                        r_outerProfileWallDef2.Points = new List<Point3D>() {
                            new Point3D(0, - pr.T4/2, d.LWL * 1000 - pr.H1),
                            new Point3D(x2, - pr.T4/2, d.LWL * 1000 - pr.H1),
                            new Point3D(x2 + pr.L3, - pr.T4/2,  d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - pr.T4 - pr.B7- pr.T3, - pr.T4/2, d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - pr.T4 - pr.B7- pr.T3, - pr.T4/2, d.HWL * 1000 + pr.H3),
                            new Point3D(0, - pr.T4/2, d.HWL * 1000 + pr.H3),
                        };
                        profileWalls.Add(r_outerProfileWallDef2);
                    }
                    else if (d.SelectedPumpingStationType == "Type3")
                    {
                        // 좌안부 Type3 벽체 계산 로직
                        // 좌안부 외벽 - 진입부 반대측 프로파일 (긴 벽체, Type1, Type3) - W1
                        var r_outerProfileWallDef2 = new ProfileWallDefinition
                        {
                            Thickness = pr.T4,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "W1",
                            Zone = "",
                            Part = ""
                        };
                        r_outerProfileWallDef2.Points = new List<Point3D>() {
                            new Point3D(0, -pr.T4/2, d.LWL * 1000 - pr.H1),
                            new Point3D(x2, -pr.T4/2, d.LWL * 1000 - pr.H1),
                            new Point3D(x2 + pr.L3, -pr.T4/2,  d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - pr.T4, -pr.T4/2, d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - pr.T4, -pr.T4/2, d.HWL * 1000 + pr.H3),
                            new Point3D(0, -pr.T4/2, d.HWL * 1000 + pr.H3),
                        };
                        profileWalls.Add(r_outerProfileWallDef2);
                    }
                    break;
                case "측면부":
                    if (d.SelectedPumpingStationType == "Type1")
                    {
                        // 측면부 Type1 벽체 계산 로직
                        // 측면부 외벽 -  프로파일 (긴 벽체, Type1, Type3) - W1
                        var s_outerProfileWallDef2 = new ProfileWallDefinition
                        {
                            Thickness = pr.T4,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "W1",
                            Zone = "",
                            Part = ""
                        };
                        s_outerProfileWallDef2.Points = new List<Point3D>() {
                            new Point3D(0, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2, d.LWL * 1000 - pr.H1),
                            new Point3D(x2, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2, d.LWL * 1000 - pr.H1),
                            new Point3D(x2 + pr.L3, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2,  d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - pr.T4, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2, d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - pr.T4, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2, d.HWL * 1000 + pr.H3),
                            new Point3D(0, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2, d.HWL * 1000 + pr.H3),
                        };
                        profileWalls.Add(s_outerProfileWallDef2);

                        // 측면부 외벽 - 프로파일 (긴 벽체, Type1, Type3) - W1
                        var s_outerProfileWallDef3 = new ProfileWallDefinition
                        {
                            Thickness = pr.T4,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "W1",
                            Zone = "",
                            Part = ""
                        };
                        s_outerProfileWallDef3.Points = new List<Point3D>() {
                            new Point3D(0, - pr.T4/2, d.LWL * 1000 - pr.H1),
                            new Point3D(x2, - pr.T4/2, d.LWL * 1000 - pr.H1),
                            new Point3D(x2 + pr.L3, - pr.T4/2,  d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - pr.T4, - pr.T4/2, d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - pr.T4, - pr.T4/2, d.HWL * 1000 + pr.H3),
                            new Point3D(0, - pr.T4/2, d.HWL * 1000 + pr.H3),
                        };
                        profileWalls.Add(s_outerProfileWallDef3);
                    }
                    else if (d.SelectedPumpingStationType == "Type2")
                    {
                        // 측면부 Type2 벽체 계산 로직
                        // 측면부 외벽1 -  프로파일 (긴 벽체, Type2) - W1
                        var s_outerProfileWallDef2 = new ProfileWallDefinition
                        {
                            Thickness = pr.T4,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "W1",
                            Zone = "",
                            Part = ""
                        };
                        s_outerProfileWallDef2.Points = new List<Point3D>() {
                            new Point3D(0, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2, d.LWL * 1000 - pr.H1),
                            new Point3D(x2, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2, d.LWL * 1000 - pr.H1),
                            new Point3D(x2 + pr.L3, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2,  d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2, d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2, d.HWL * 1000 + pr.H3),
                            new Point3D(0, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2, d.HWL * 1000 + pr.H3),
                        };
                        profileWalls.Add(s_outerProfileWallDef2);

                        // 측면부 외벽2 - 프로파일 (긴 벽체, Type2) - W1
                        var s_outerProfileWallDef3 = new ProfileWallDefinition
                        {
                            Thickness = pr.T4,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "W1",
                            Zone = "",
                            Part = ""
                        };
                        s_outerProfileWallDef3.Points = new List<Point3D>() {
                            new Point3D(0, - pr.T4/2, d.LWL * 1000 - pr.H1),
                            new Point3D(x2, - pr.T4/2, d.LWL * 1000 - pr.H1),
                            new Point3D(x2 + pr.L3, - pr.T4/2,  d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - pr.T4 - pr.B7- pr.T3, - pr.T4/2, d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - pr.T4 - pr.B7- pr.T3, - pr.T4/2, d.HWL * 1000 + pr.H3),
                            new Point3D(0, - pr.T4/2, d.HWL * 1000 + pr.H3),
                        };
                        profileWalls.Add(s_outerProfileWallDef3);
                    }
                    else if (d.SelectedPumpingStationType == "Type3")
                    {
                        // 측면부 Type3 벽체 계산 로직
                        // 측면부 외벽 -  프로파일 (긴 벽체, Type1, Type3) - W1
                        var s_outerProfileWallDef2 = new ProfileWallDefinition
                        {
                            Thickness = pr.T4,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "W1",
                            Zone = "",
                            Part = ""
                        };
                        s_outerProfileWallDef2.Points = new List<Point3D>() {
                            new Point3D(0, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2, d.LWL * 1000 - pr.H1),
                            new Point3D(x2, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2, d.LWL * 1000 - pr.H1),
                            new Point3D(x2 + pr.L3, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2,  d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - pr.T4, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2, d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - pr.T4, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2, d.HWL * 1000 + pr.H3),
                            new Point3D(0, pl.B8 * d.N + pl.T5 * (d.N-1) + pr.T4/2, d.HWL * 1000 + pr.H3),
                        };
                        profileWalls.Add(s_outerProfileWallDef2);

                        // 측면부 외벽 - 프로파일 (긴 벽체, Type1, Type3) - W1
                        var s_outerProfileWallDef3 = new ProfileWallDefinition
                        {
                            Thickness = pr.T4,
                            LevelName = FoundationPumpLevelName,
                            ElementCode = "W1",
                            Zone = "",
                            Part = ""
                        };
                        s_outerProfileWallDef3.Points = new List<Point3D>() {
                            new Point3D(0, - pr.T4/2, d.LWL * 1000 - pr.H1),
                            new Point3D(x2, - pr.T4/2, d.LWL * 1000 - pr.H1),
                            new Point3D(x2 + pr.L3, - pr.T4/2,  d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - pr.T4, - pr.T4/2, d.LWL * 1000 - pr.H4),
                            new Point3D(totalLength - pr.T4, - pr.T4/2, d.HWL * 1000 + pr.H3),
                            new Point3D(0, - pr.T4/2, d.HWL * 1000 + pr.H3),
                        };
                        profileWalls.Add(s_outerProfileWallDef3);
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
            //var ts = dto.TypeSelectionDto;
            var totalLength = pr.B1 + pr.B2 + pr.B3 + pr.B4 + pr.B5 + pr.B6 + pr.T3 + pr.B7 + pr.T4;
            var totalWidth = pr.T4 * 2 + (pl.B8 * d.N) + (pl.T5 * (d.N - 1));
            double x2 = totalLength - pr.T4 - pr.B7 - pr.T3 - pr.B6 - pr.B5 / 2 - pr.L4 - pr.L3;

            var beamDefs = new List<BeamDefinition>();
            for (int i = 0; i < d.N; i++)
            {
                var beamDef1 = new BeamDefinition()
                {
                    StartPoint = new Point3D(pr.B1 + pr.B2 + pr.GB1 / 2, (pl.B8 + pl.T5) * i, d.HWL * 1000 + pr.H3 + pr.T1 - pr.GH1 / 2),
                    EndPoint = new Point3D(pr.B1 + pr.B2 + pr.GB1 / 2, (pl.B8 + pl.T5) * i + pl.B8, d.HWL * 1000 + pr.H3 + pr.T1 - pr.GH1 / 2),
                    Width = pr.GB1,
                    Height = pr.GH1,
                    LevelName = UpperSlabLevelName,

                    ElementCode = "G1",
                    Zone = "",
                    Part = "상부 거더",
                };
                var beamDef2 = new BeamDefinition()
                {
                    StartPoint = new Point3D(pr.B1 + pr.B2 + pr.B3 - pr.GB1 / 2, (pl.B8 + pl.T5) * i, d.HWL * 1000 + pr.H3 + pr.T1 - pr.GH1 / 2),
                    EndPoint = new Point3D(pr.B1 + pr.B2 + pr.B3 - pr.GB1 / 2, (pl.B8 + pl.T5) * i + pl.B8, d.HWL * 1000 + pr.H3 + pr.T1 - pr.GH1 / 2),
                    Width = pr.GB1,
                    Height = pr.GH1,
                    LevelName = UpperSlabLevelName,

                    ElementCode = "G1",
                    Zone = "",
                    Part = "상부 거더",
                };
                var beamDef3 = new BeamDefinition()
                {
                    StartPoint = new Point3D(pr.B1 + pr.B2 + pr.B3 + pr.B4 - pr.GB1 / 2, (pl.B8 + pl.T5) * i, d.HWL * 1000 + pr.H3 + pr.T1 - pr.GH1 / 2),
                    EndPoint = new Point3D(pr.B1 + pr.B2 + pr.B3 + pr.B4 - pr.GB1 / 2, (pl.B8 + pl.T5) * i + pl.B8, d.HWL * 1000 + pr.H3 + pr.T1 - pr.GH1 / 2),
                    Width = pr.GB1,
                    Height = pr.GH1,
                    LevelName = UpperSlabLevelName,

                    ElementCode = "G1",
                    Zone = "",
                    Part = "상부 거더",
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
            //var ts = dto.TypeSelectionDto;

            var totalLength = pr.B1 + pr.B2 + pr.B3 + pr.B4 + pr.B5 + pr.B6 + pr.T3 + pr.B7 + pr.T4;
            var totalWidth = pr.T4 * 2 + (pl.B8 * d.N) + (pl.T5 * (d.N - 1));
            double x2 = totalLength - pr.T4 - pr.B7 - pr.T3 - pr.B6 - pr.B5 / 2 - pr.L4 - pr.L3;
            double subThk = 100; // 버림 두께 

            var solidExtrusionDefs = new List<SolidExtrusionDefinition>();

            // 공통 - 기초
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
                                                new Point3D(0,                                                  -pr.T4 - pl.B10, d.LWL*1000 - pr.H1),
                                                new Point3D(x2,                                                 -pr.T4 - pl.B10, d.LWL*1000 - pr.H1),
                                                new Point3D(x2 + pr.L3,                                         -pr.T4 - pl.B10, d.LWL*1000 - pr.H4),
                                                new Point3D(totalLength + pl.B10,                               -pr.T4 - pl.B10, d.LWL*1000 - pr.H4),
                                                new Point3D(totalLength + pl.B10,                               -pr.T4 - pl.B10, d.LWL*1000 - pr.H4 - pr.T2),
                                                new Point3D(x2 + pr.L3 - pr.T2 * Math.Tan(calculatedTheta / 2), -pr.T4 - pl.B10, d.LWL*1000 - pr.H4 - pr.T2),
                                                new Point3D(x2 - pr.T2 * Math.Tan(calculatedTheta / 2),         -pr.T4 - pl.B10, d.LWL*1000 - pr.H1 - pr.T2),
                                                new Point3D(0,                                                  -pr.T4 - pl.B10, d.LWL*1000 - pr.H1 - pr.T2),
                                            };
                    subBaseSolid.Profile = new List<Point3D>()
                                            {
                                                new Point3D(- subThk,                                                  -pr.T4 - pl.B10- subThk, d.LWL*1000 - pr.H1 - pr.T2),
                                                new Point3D(x2 - pr.T2 * Math.Tan(calculatedTheta / 2),                                                 -pr.T4 - pl.B10- subThk, d.LWL*1000 - pr.H1 - pr.T2),
                                                new Point3D(x2 + pr.L3 - pr.T2 * Math.Tan(calculatedTheta / 2),                                         -pr.T4 - pl.B10- subThk, d.LWL*1000 - pr.H4- pr.T2),
                                                new Point3D(totalLength + pl.B10 + subThk,                               -pr.T4 - pl.B10- subThk, d.LWL*1000 - pr.H4- pr.T2),
                                                new Point3D(totalLength + pl.B10 + subThk,                               -pr.T4 - pl.B10- subThk, d.LWL*1000 - pr.H4 - pr.T2 - subThk),
                                                new Point3D(x2 + pr.L3 - (pr.T2 + subThk) * Math.Tan(calculatedTheta / 2), -pr.T4 - pl.B10- subThk, d.LWL*1000 - pr.H4 - pr.T2- subThk),
                                                new Point3D(x2 - (pr.T2 + subThk) * Math.Tan(calculatedTheta / 2),         -pr.T4 - pl.B10- subThk, d.LWL*1000 - pr.H1 - pr.T2- subThk),
                                                new Point3D(- subThk,                                                  -pr.T4 - pl.B10- subThk, d.LWL*1000 - pr.H1 - pr.T2- subThk),
                                            };
                    break;
                case "Type2":
                    fndBaseSolid.Profile = new List<Point3D>()
                                            {
                                                new Point3D(0,                                                  -pr.T4 - pl.B10, d.LWL*1000 - pr.H1),
                                                new Point3D(x2,                                                 -pr.T4 - pl.B10, d.LWL*1000 - pr.H1),
                                                new Point3D(x2 + pr.L3,                                         -pr.T4 - pl.B10, d.LWL*1000 - pr.H4),
                                                new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3 + pr.T4+ pl.B10,        -pr.T4 - pl.B10, d.LWL*1000 - pr.H4),
                                                new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3 + pr.T4+ pl.B10,        -pr.T4 - pl.B10, d.LWL*1000 - pr.H4 - pr.T2),
                                                new Point3D(x2 + pr.L3 - pr.T2 * Math.Tan(calculatedTheta / 2), -pr.T4 - pl.B10, d.LWL*1000 - pr.H4 - pr.T2),
                                                new Point3D(x2 - pr.T2 * Math.Tan(calculatedTheta / 2),         -pr.T4 - pl.B10, d.LWL*1000 - pr.H1 - pr.T2),
                                                new Point3D(0,                                                  -pr.T4 - pl.B10, d.LWL*1000 - pr.H1 - pr.T2),
                                            };
                    subBaseSolid.Profile = new List<Point3D>()
                                            {
                                                new Point3D(- subThk,                                                      -pr.T4 - pl.B10- subThk, d.LWL*1000 - pr.H1 - pr.T2),
                                                new Point3D(x2 - pr.T2 * Math.Tan(calculatedTheta / 2),                    -pr.T4 - pl.B10- subThk, d.LWL*1000 - pr.H1 - pr.T2),
                                                new Point3D(x2 + pr.L3 - pr.T2 * Math.Tan(calculatedTheta / 2),            -pr.T4 - pl.B10- subThk, d.LWL*1000 - pr.H4- pr.T2),
                                                new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3 + pr.T4+ pl.B10 + subThk,                          -pr.T4 - pl.B10- subThk, d.LWL*1000 - pr.H4- pr.T2),
                                                new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3 + pr.T4+ pl.B10 + subThk,                          -pr.T4 - pl.B10- subThk, d.LWL*1000 - pr.H4 - pr.T2 - subThk),
                                                new Point3D(x2 + pr.L3 - (pr.T2 + subThk) * Math.Tan(calculatedTheta / 2), -pr.T4 - pl.B10- subThk, d.LWL*1000 - pr.H4 - pr.T2- subThk),
                                                new Point3D(x2 - (pr.T2 + subThk) * Math.Tan(calculatedTheta / 2),         -pr.T4 - pl.B10- subThk, d.LWL*1000 - pr.H1 - pr.T2- subThk),
                                                new Point3D(- subThk,                                                      -pr.T4 - pl.B10- subThk, d.LWL*1000 - pr.H1 - pr.T2- subThk),
                                            };
                    break;
                case "Type3":
                    fndBaseSolid.Profile = new List<Point3D>()
                                            {
                                                new Point3D(0,                                                  -pr.T4 - pl.B10, d.LWL*1000 - pr.H1),
                                                new Point3D(x2,                                                 -pr.T4 - pl.B10, d.LWL*1000 - pr.H1),
                                                new Point3D(x2 + pr.L3,                                         -pr.T4 - pl.B10, d.LWL*1000 - pr.H4),
                                                new Point3D(totalLength + pl.B10,                               -pr.T4 - pl.B10, d.LWL*1000 - pr.H4),
                                                new Point3D(totalLength + pl.B10,                               -pr.T4 - pl.B10, d.LWL*1000 - pr.H4 - pr.T2),
                                                new Point3D(x2 + pr.L3 - pr.T2 * Math.Tan(calculatedTheta / 2), -pr.T4 - pl.B10, d.LWL*1000 - pr.H4 - pr.T2),
                                                new Point3D(x2 - pr.T2 * Math.Tan(calculatedTheta / 2),         -pr.T4 - pl.B10, d.LWL*1000 - pr.H1 - pr.T2),
                                                new Point3D(0,                                                  -pr.T4 - pl.B10, d.LWL*1000 - pr.H1 - pr.T2),
                                            };
                    subBaseSolid.Profile = new List<Point3D>()
                                            {
                                                new Point3D(- subThk,                                                     -pr.T4 - pl.B10- subThk, d.LWL*1000 - pr.H1 - pr.T2),
                                                new Point3D(x2 - pr.T2 * Math.Tan(calculatedTheta / 2),                   -pr.T4 - pl.B10- subThk, d.LWL*1000 - pr.H1 - pr.T2),
                                                new Point3D(x2 + pr.L3 - pr.T2 * Math.Tan(calculatedTheta / 2),           -pr.T4 - pl.B10- subThk, d.LWL*1000 - pr.H4- pr.T2),
                                                new Point3D(totalLength + pl.B10 + subThk,                                -pr.T4 - pl.B10- subThk, d.LWL*1000 - pr.H4- pr.T2),
                                                new Point3D(totalLength + pl.B10 + subThk,                                -pr.T4 - pl.B10- subThk, d.LWL*1000 - pr.H4 - pr.T2 - subThk),
                                                new Point3D(x2 + pr.L3 - (pr.T2 + subThk) * Math.Tan(calculatedTheta / 2),-pr.T4 - pl.B10- subThk, d.LWL*1000 - pr.H4 - pr.T2- subThk),
                                                new Point3D(x2 - (pr.T2 + subThk) * Math.Tan(calculatedTheta / 2),        -pr.T4 - pl.B10- subThk, d.LWL*1000 - pr.H1 - pr.T2- subThk),
                                                new Point3D(- subThk,                                                     -pr.T4 - pl.B10- subThk, d.LWL*1000 - pr.H1 - pr.T2- subThk),
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
                    stairPts.Add(new Point3D(x2 + threadWidth * j, (pl.B8 + pl.T5) * i, d.LWL * 1000 - pr.H1 - riserHeight * j));
                    stairPts.Add(new Point3D(x2 + threadWidth * (j + 1), (pl.B8 + pl.T5) * i, d.LWL * 1000 - pr.H1 - riserHeight * j));
                }
                stairPts.Add(new Point3D(x2 + threadWidth * pr.NS, (pl.B8 + pl.T5) * i, d.LWL * 1000 - pr.H4 - 100));   // CurveLoop 오류 막기위해 마지막 100mm 여유

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
                                        new Point3D(0,              (pl.B8 + pl.T5)* i, d.LWL*1000),
                                        new Point3D(pr.L1,          (pl.B8 + pl.T5)* i, d.LWL*1000),
                                        new Point3D(pr.L1 + pr.L2,  (pl.B8 + pl.T5)* i, d.LWL*1000 - pr.H1),
                                        new Point3D(0,              (pl.B8 + pl.T5)* i, d.LWL*1000 - pr.H1),
                                    },
                    Normal = new Vector3D(0, 1, 0),
                    Distance = pl.B8,
                    ElementCode = "F1",
                    Zone = "",
                    Part = "유입부 턱",
                };

                solidExtrusionDefs.Add(inletCurb);
            }

            // 진입부 기초 및 버림 추가
            switch (d.SelectedEntranceType)
            {
                case "우안부":
                    var rightFndDef = new SolidExtrusionDefinition
                    {
                        Profile = new List<Point3D>()
                        {
                            new Point3D(totalLength + pl.B10,                     -(pr.T4 + pl.B10),                          d.LWL*1000 - pr.H4),
                            new Point3D(totalLength + pl.B10,                     -pl.T5 - pl.B9 - pr.T4 - pl.B10, d.LWL*1000 - pr.H4),
                            new Point3D(totalLength - pr.T4 * 2 - pl.L5 - pl.B10, -pl.T5 - pl.B9 - pr.T4 - pl.B10, d.LWL*1000 - pr.H4),
                            new Point3D(totalLength - pr.T4 * 2 - pl.L5 - pl.B10, -(pr.T4 + pl.B10),                          d.LWL*1000 - pr.H4),
                        },
                        Normal = new Vector3D(0, 0, -1),
                        Distance = pr.T2,
                        ElementCode = "F1",
                        Zone = "",
                        Part = "",
                    };
                    solidExtrusionDefs.Add(rightFndDef);

                    var rightSubFndDef = new SolidExtrusionDefinition
                    {
                        Profile = new List<Point3D>()
                        {
                            new Point3D(totalLength + pl.B10 + subThk,                     -(pr.T4 + pl.B10 + subThk),                         d.LWL*1000 - pr.T2 - pr.H4),
                            new Point3D(totalLength + pl.B10 + subThk,                     -pl.T5 - pl.B9 - pr.T4 - pl.B10 - subThk, d.LWL*1000 - pr.T2 - pr.H4),
                            new Point3D(totalLength - pr.T4 * 2 - pl.L5 - pl.B10 - subThk, -pl.T5 - pl.B9 - pr.T4 - pl.B10 - subThk, d.LWL*1000 - pr.T2 - pr.H4),
                            new Point3D(totalLength - pr.T4 * 2 - pl.L5 - pl.B10 - subThk, -(pr.T4 + pl.B10 + subThk),                         d.LWL*1000 - pr.T2 - pr.H4),
                        },
                        Normal = new Vector3D(0, 0, -1),
                        Distance = subThk,
                        ElementCode = "F2",
                        Zone = "",
                        Part = "",
                    };
                    solidExtrusionDefs.Add(rightSubFndDef);

                    break;
                case "좌안부":
                    var leftFndDef = new SolidExtrusionDefinition
                    {
                        Profile = new List<Point3D>()
                        {
                            new Point3D(totalLength + pl.B10,                     totalWidth - pr.T4  + (pl.B10),                          d.LWL*1000 - pr.H4),
                            new Point3D(totalLength + pl.B10,                     totalWidth - pr.T4 * 2 - (-pl.T5 - pl.B9 - pr.T4 - pl.B10), d.LWL*1000 - pr.H4),
                            new Point3D(totalLength - pr.T4 * 2 - pl.L5 - pl.B10, totalWidth - pr.T4 * 2 - (-pl.T5 - pl.B9 - pr.T4 - pl.B10), d.LWL*1000 - pr.H4),
                            new Point3D(totalLength - pr.T4 * 2 - pl.L5 - pl.B10, totalWidth - pr.T4  + (pl.B10),                          d.LWL*1000 - pr.H4),
                        },
                        Normal = new Vector3D(0, 0, -1),
                        Distance = pr.T2,
                        ElementCode = "F1",
                        Zone = "",
                        Part = "",
                    };
                    solidExtrusionDefs.Add(leftFndDef);

                    var leftSubFndDef = new SolidExtrusionDefinition
                    {
                        Profile = new List<Point3D>()
                        {
                            new Point3D(totalLength + pl.B10 + subThk,                     totalWidth - pr.T4  +(pl.B10 + subThk),                          d.LWL*1000 - pr.T2 - pr.H4),
                            new Point3D(totalLength + pl.B10 + subThk,                     totalWidth - pr.T4 * 2 -(-pl.T5 - pl.B9 - pr.T4 - pl.B10 - subThk), d.LWL*1000 - pr.T2 - pr.H4),
                            new Point3D(totalLength - pr.T4 * 2 - pl.L5 - pl.B10 - subThk, totalWidth - pr.T4 * 2 -(-pl.T5 - pl.B9 - pr.T4 - pl.B10 - subThk), d.LWL*1000 - pr.T2 - pr.H4),
                            new Point3D(totalLength - pr.T4 * 2 - pl.L5 - pl.B10 - subThk, totalWidth - pr.T4  +(pl.B10 + subThk),                          d.LWL*1000 - pr.T2 - pr.H4),
                        },
                        Normal = new Vector3D(0, 0, -1),
                        Distance = subThk,
                        ElementCode = "F2",
                        Zone = "",
                        Part = "",
                    };
                    solidExtrusionDefs.Add(leftSubFndDef);
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
            //var ts = dto.TypeSelectionDto;
            var totalLength = pr.B1 + pr.B2 + pr.B3 + pr.B4 + pr.B5 + pr.B6 + pr.T3 + pr.B7 + pr.T4;
            var totalWidth = pr.T4 * 2 + (pl.B8 * d.N) + (pl.T5 * (d.N - 1));
            double x2 = totalLength - pr.T4 - pr.B7 - pr.T3 - pr.B6 - pr.B5 / 2 - pr.L4 - pr.L3;

            var openings = new List<RectangularSlabOpeningDefinition>();

            for (int i = 0; i < d.N; i++)
            {

                if (pr.IsRectangularOpening)
                {
                    var pumpOpening = new RectangularSlabOpeningDefinition
                    {
                        Width = pr.B5,
                        Length = pr.B5,
                        Position = new Point2D(totalLength - pr.T4 - pr.B7 - pr.T3 - pr.B6 - pr.B5 / 2, pl.B8 / 2 + (pl.B8 + pl.T5) * i),

                        LevelName = UpperSlabLevelName,
                        Name = "",
                        HostElementCode = "S1",
                    };
                    openings.Add(pumpOpening);
                }

                // 제진기 오프닝
                var screenOpening = new RectangularSlabOpeningDefinition
                {
                    Width = pr.B2,
                    Length = pl.B8,
                    Position = new Point2D(pr.B1 + pr.B2 / 2, pl.B8 / 2 + (pl.B8 + pl.T5) * i),

                    LevelName = UpperSlabLevelName,
                    Name = "",
                    HostElementCode = "S1",
                };
                openings.Add(screenOpening);
            }

            //// 밸브실 상부 오프닝
            //var valveRoomOpening = new RectangularSlabOpeningDefinition
            //{
            //    Width = pr.B7,
            //    Length = d.N * pl.B8 + (d.N - 1) * pl.T5,
            //    Position = new Point2D(totalLength - pr.T4 - pr.B7 / 2, (totalWidth - pr.T4 * 2) / 2),
            //    LevelName = UpperSlabLevelName,
            //    Name = "",
            //    HostElementCode = "S1",
            //};
            //openings.Add(valveRoomOpening);

            return openings;
        }
        public static IReadOnlyList<CircularSlabOpeningDefinition> CalculateCircularSlabOpenings(PumpCreationRequestDto dto)
        {
            var d = dto.DesignConditionDto;
            var pr = dto.ProfileSpecDto;
            var pl = dto.PlanSpecDto;
            //var ts = dto.TypeSelectionDto;
            var totalLength = pr.B1 + pr.B2 + pr.B3 + pr.B4 + pr.B5 + pr.B6 + pr.T3 + pr.B7 + pr.T4;
            var totalWidth = pr.T4 * 2 + (pl.B8 * d.N) + (pl.T5 * (d.N - 1));
            double x2 = totalLength - pr.T4 - pr.B7 - pr.T3 - pr.B6 - pr.B5 / 2 - pr.L4 - pr.L3;

            var openings = new List<CircularSlabOpeningDefinition>();


            if (!pr.IsRectangularOpening)
            {
                for (int i = 0; i < d.N; i++)
                {
                    var pumpOpening = new CircularSlabOpeningDefinition
                    {
                        Diameter = pr.B5,
                        Position = new Point2D(totalLength - pr.T4 - pr.B7 - pr.T3 - pr.B6 - pr.B5 / 2, pl.B8 / 2 + (pl.B8 + pl.T5) * i),

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
            //var ts = dto.TypeSelectionDto;
            var totalLength = pr.B1 + pr.B2 + pr.B3 + pr.B4 + pr.B5 + pr.B6 + pr.T3 + pr.B7 + pr.T4;
            var totalWidth = pr.T4 * 2 + (pl.B8 * d.N) + (pl.T5 * (d.N - 1));
            var x2 = totalLength - pr.T4 - pr.B7 - pr.T3 - pr.B6 - pr.B5 / 2 - pr.L4 - pr.L3;

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
                Position = new Point3D(totalLength - pr.T4 - pl.L5 + pr.OB1 / 2, 0, 0),

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
            //var ts = dto.TypeSelectionDto;
            var totalLength = pr.B1 + pr.B2 + pr.B3 + pr.B4 + pr.B5 + pr.B6 + pr.T3 + pr.B7 + pr.T4;
            var totalWidth = pr.T4 * 2 + (pl.B8 * d.N) + (pl.T5 * (d.N - 1));
            var x2 = totalLength - pr.T4 - pr.B7 - pr.T3 - pr.B6 - pr.B5 / 2 - pr.L4 - pr.L3;

            var openings = new List<CircularWallOpeningDefinition>();

            // 밸브실 외벽 오프닝
            for (int i = 0; i < d.N; i++)
            {
                var wallOpening = new CircularWallOpeningDefinition
                {
                    Diameter = d.D,
                    //Position = new Point3D(totalLength - pr.T4 / 2, pl.B8 / 2 + (pl.B8 + pl.T5) * i, 0),
                    Position = new Point3D(0, pl.B8 / 2 + (pl.B8 + pl.T5) * i, 0),

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
                    //Position = new Point3D(totalLength - pr.T4 / 2, pl.B8 / 2 + (pl.B8 + pl.T5) * i, 0),
                    Position = new Point3D(0, pl.B8 / 2 + (pl.B8 + pl.T5) * i, 0),

                    LevelName = ValveRoomLevelName,
                    Name = "",
                    HostElementCode = "W4",
                    OffsetZ = pr.H6
                };
                openings.Add(wallOpening);
            }
            return openings;
        }
        public static IReadOnlyList<SectionViewDefinition> CalculateSectionViews(PumpCreationRequestDto dto)
        {
            var d = dto.DesignConditionDto;
            var pr = dto.ProfileSpecDto;
            var pl = dto.PlanSpecDto;
            var totalLength = pr.B1 + pr.B2 + pr.B3 + pr.B4 + pr.B5 + pr.B6 + pr.T3 + pr.B7 + pr.T4;
            var totalWidth = pr.T4 * 2 + (pl.B8 * d.N) + (pl.T5 * (d.N - 1));

            var sectionViewDefs = new List<SectionViewDefinition>();

            var offset = 500; // 여유치 (mm)

            sectionViewDefs.Add(new SectionViewDefinition
            {
                Name = "A",
                Min = new Point3D(-offset, pl.B8 / 2, d.LWL * 1000 - (pr.H4 + pr.T2 + 100 + offset)),
                Max = new Point3D(totalLength + pl.B10 + offset, pl.B8 / 2 + offset, d.HWL * 1000 + pr.H3 + pr.T1 + offset),

                BasisX = d.SelectedEntranceType == "좌안부" ? new Vector3D(-1, 0, 0) : new Vector3D(1, 0, 0),
                BasisZ = d.SelectedEntranceType == "좌안부" ? new Vector3D(0, -1, 0) : new Vector3D(0, 1, 0),
                //Flip = true
            });

            sectionViewDefs.Add(new SectionViewDefinition
            {
                Name = "B",
                Min = new Point3D(-offset, pl.B8 - 100, d.LWL * 1000 - (pr.H4 + pr.T2 + 100 + offset)),
                Max = new Point3D(totalLength + pl.B10 + offset, pl.B8 + pl.T5 + 100, d.HWL * 1000 + pr.H3 + pr.T1 + offset),

                BasisX = d.SelectedEntranceType == "좌안부" ? new Vector3D(-1, 0, 0) : new Vector3D(1, 0, 0),
                BasisZ = d.SelectedEntranceType == "좌안부" ? new Vector3D(0, -1, 0) : new Vector3D(0, 1, 0),
                //Flip = true
            });

            var entranceSectionLength = (pl.B10 + pr.T4) * 2 + pl.L5;

            if (d.SelectedEntranceType == "우안부")
            {
                sectionViewDefs.Add(new SectionViewDefinition
                {
                    Name = "C",
                    Min = new Point3D(totalLength - pr.T4 * 2 - pl.L5 - pl.B10 - offset, -pl.T5 - pl.B9 + 100, d.LWL * 1000 - (pr.H4 + pr.T2 + 100 + offset)),
                    Max = new Point3D(totalLength + pl.B10 + offset, -pl.T5 - pl.B9 + offset, d.HWL * 1000 + pr.H3 + pr.T1 + offset),

                    BasisX = new Vector3D(1, 0, 0),
                    BasisZ = new Vector3D(0, 1, 0),
                    //Flip = true
                });

                sectionViewDefs.Add(new SectionViewDefinition
                {
                    Name = "D",
                    Min = new Point3D(totalLength - pr.T4 * 2 - pl.L5 - pl.B10 - offset, -pl.T5 - offset, d.LWL * 1000 - (pr.H4 + pr.T2 + 100 + offset)),
                    Max = new Point3D(totalLength + pl.B10 + offset, -pl.T5 + offset, d.HWL * 1000 + pr.H3 + pr.T1 + offset),

                    BasisX = new Vector3D(1, 0, 0),
                    BasisZ = new Vector3D(0, 1, 0),
                    //Flip = true
                });

                sectionViewDefs.Add(new SectionViewDefinition
                {
                    Name = "E",
                    Min = new Point3D(totalLength - pr.T4 * 2 - pl.L5 - pl.B10 - offset, -pl.T5 - pl.B9 - pr.T4 - pl.B10 - offset, d.LWL * 1000 - (pr.H4 + pr.T2 + 100 + offset)),
                    Max = new Point3D(totalLength + pl.B10 + offset, +offset, d.HWL * 1000 + pr.H3 / 2),

                    BasisX = new Vector3D(-1, 0, 0),
                    BasisZ = new Vector3D(0, 0, -1),
                    //Flip = true
                });
            }

            if (d.SelectedEntranceType == "좌안부")
            {
                sectionViewDefs.Add(new SectionViewDefinition
                {
                    Name = "C",
                    Min = new Point3D(totalLength - pr.T4 * 2 - pl.L5 - pl.B10 - offset, (totalWidth - pr.T4) - (-pl.T5 - pl.B9 + 100), d.LWL * 1000 - (pr.H4 + pr.T2 + 100 + offset)),
                    Max = new Point3D(totalLength + pl.B10 + offset, (totalWidth - pr.T4) - (-pl.T5 - pl.B9 + offset), d.HWL * 1000 + pr.H3 + pr.T1 + offset),

                    BasisX = new Vector3D(-1, 0, 0),
                    BasisZ = new Vector3D(0, -1, 0),
                    //Flip = true
                });

                sectionViewDefs.Add(new SectionViewDefinition
                {
                    Name = "D",
                    Min = new Point3D(totalLength - pr.T4 * 2 - pl.L5 - pl.B10 - offset, (totalWidth - pr.T4) - (-pl.T5 - offset), d.LWL * 1000 - (pr.H4 + pr.T2 + 100 + offset)),
                    Max = new Point3D(totalLength + pl.B10 + offset, (totalWidth - pr.T4) - (-pl.T5 + offset), d.HWL * 1000 + pr.H3 + pr.T1 + offset),

                    BasisX = new Vector3D(-1, 0, 0),
                    BasisZ = new Vector3D(0, -1, 0),
                    //Flip = true
                });

                sectionViewDefs.Add(new SectionViewDefinition
                {
                    Name = "E",
                    Min = new Point3D(totalLength - pr.T4 * 2 - pl.L5 - pl.B10 - offset, (totalWidth - pr.T4) - (-pl.T5 - pl.B9 - pr.T4 - pl.B10 - offset), d.LWL * 1000 - (pr.H4 + pr.T2 + 100 + offset)),
                    Max = new Point3D(totalLength + pl.B10 + offset, (totalWidth - pr.T4) - (offset), d.HWL * 1000 + pr.H3 / 2),

                    BasisX = new Vector3D(-1, 0, 0),
                    BasisZ = new Vector3D(0, 0, -1),
                    //Flip = true
                });
            }

            sectionViewDefs.Add(new SectionViewDefinition
            {
                Name = "F",
                Min = new Point3D(pr.L1 / 2, -(pr.T4 + pl.B10 + offset), d.LWL * 1000 - (pr.H1 + pr.T2 + 100 + offset)),
                Max = new Point3D(pr.L1 + pr.L2, totalWidth - pr.T4 + pl.B10 + 100 + offset, d.HWL * 1000 + pr.H3 + pr.T1 + offset),

                BasisX = new Vector3D(0, -1, 0),
                BasisZ = new Vector3D(1, 0, 0),
                //Flip = true
            });

            sectionViewDefs.Add(new SectionViewDefinition
            {
                Name = "G",
                Min = new Point3D(pr.B1 + pr.B2 - offset, -(pr.T4 + pl.B10 + offset), d.LWL * 1000 - (pr.H1 + pr.T2 + 100 + offset)),
                Max = new Point3D(pr.B1 + pr.B2 + pr.GB1, totalWidth - pr.T4 + pl.B10 + 100 + offset, d.HWL * 1000 + pr.H3 + pr.T1 + offset),

                BasisX = new Vector3D(0, -1, 0),
                BasisZ = new Vector3D(1, 0, 0),
                //Flip = true
            });

            double minWidth = d.SelectedEntranceType switch
            {
                "좌안부" => -(pr.T4 + pl.B10 + offset),
                "우안부" => -(pl.T5 + pl.B9 + pr.T4 + pl.B10 + offset),
                "측면부" => -(pr.T4 + pl.B10 + offset),
                _ => throw new Exception()
            };
            double maxWidth = d.SelectedEntranceType switch
            {
                "좌안부" => totalWidth - pr.T4 * 2 + pl.T5 + pl.B9 + pr.T4 + pl.B10 + offset,
                "우안부" => totalWidth - pr.T4 + pl.B10 + 100 + offset,
                "측면부" => totalWidth - pr.T4 + pl.B10 + 100 + offset,
                _ => throw new Exception()
            };

            sectionViewDefs.Add(new SectionViewDefinition
            {
                Name = "H",
                Min = new Point3D(totalLength - pr.T4 - pl.L5 + 100,
                                  minWidth,
                                  d.LWL * 1000 - (pr.H4 + pr.T2 + 100 + offset)),
                Max = new Point3D(totalLength - pr.T4 - pl.L5 + offset,
                                  maxWidth,
                                  d.HWL * 1000 + pr.H3 + pr.T1 + offset),

                BasisX = new Vector3D(0, -1, 0),
                BasisZ = new Vector3D(1, 0, 0),
                //Flip = true
            });

            sectionViewDefs.Add(new SectionViewDefinition
            {
                Name = "I",
                Min = new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3 - pr.B6 - pr.B5 / 2,
                      minWidth,
                      d.LWL * 1000 - (pr.H4 + pr.T2 + 100 + offset)),
                Max = new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3 - pr.B6 - pr.B5 / 2 + 100,
                      maxWidth,
                      d.HWL * 1000 + pr.H3 + pr.T1 + offset),

                BasisX = new Vector3D(0, -1, 0),
                BasisZ = new Vector3D(1, 0, 0),
                //Flip = true
            });

            sectionViewDefs.Add(new SectionViewDefinition
            {
                Name = "J",
                Min = new Point3D(totalLength - pr.T4 - pr.B7 - pr.T3 - 100,
                      minWidth,
                      d.LWL * 1000 - (pr.H4 + pr.T2 + 100 + offset)),
                Max = new Point3D(totalLength - pr.T4 - pr.B7,
                      maxWidth,
                      d.HWL * 1000 + pr.H3 + pr.T1 + offset),

                BasisX = new Vector3D(0, -1, 0),
                BasisZ = new Vector3D(1, 0, 0),
                //Flip = true
            });

            sectionViewDefs.Add(new SectionViewDefinition
            {
                Name = "K",
                Min = new Point3D(totalLength - pr.T4 - offset,
                                  minWidth,
                                  d.LWL * 1000 - (pr.H4 + pr.T2 + 100 + offset)),
                Max = new Point3D(totalLength + offset,
                                  maxWidth,
                                  d.HWL * 1000 + pr.H3 + pr.T1 + offset),

                BasisX = new Vector3D(0, -1, 0),
                BasisZ = new Vector3D(1, 0, 0),
                //Flip = true
            });


            return sectionViewDefs;
        }
    }
}