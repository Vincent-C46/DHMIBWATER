using DHBIMWATER.Application.DTOs.Revit.PumpingStation;
using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Structures;

namespace DHBIMWATER.Application.Services
{
    public class ValveRoomGeometryCalculator
    {
        private const string FoundationLevelName = "기초";
        private const string UpperSlabLevelName = "상부슬래브";

        public static IReadOnlyList<LevelDefinition> CalculateLevels(PumpCreationRequestDto dto)
        {
            var d = dto.DesignConditionDto;
            var pr = dto.ProfileSpecDto;
            var upperSlab = d.HWL * 1000 + pr.H3;

            return new List<LevelDefinition>
            {
                new LevelDefinition { Name = FoundationLevelName, Elevation = upperSlab - pr.H7 - d.D - pr.H6 - pr.T3 },
                new LevelDefinition { Name = UpperSlabLevelName, Elevation = upperSlab },
            };
        }

        public static IReadOnlyList<SlabDefinition> CalculateSlabs(PumpCreationRequestDto dto)
        {
            var pr = dto.ProfileSpecDto;
            var size = GetSampleSize(dto);
            var upperSlabZ = GetUpperSlabElevation(dto);

            return new List<SlabDefinition>
            {
                new SlabDefinition
                {
                    Thickness = pr.T1,
                    ElevationZ = upperSlabZ,
                    LevelName = UpperSlabLevelName,
                    ElementCode = "S1",
                    Zone = "밸브실",
                    Part = "상부슬래브",
                    Points = Rectangle2D(0, 0, size.Length, size.Width),
                    SubPoints = Array.Empty<Point2D>(),
                }
            };
        }

        public static IReadOnlyList<LinearWallDefinition> CalculateLinearWalls(PumpCreationRequestDto dto)
        {
            var pr = dto.ProfileSpecDto;
            var size = GetSampleSize(dto);

            return new List<LinearWallDefinition>
            {
                new LinearWallDefinition
                {
                    Thickness = pr.T4,
                    Height = GetValveRoomHeight(dto),
                    BaseOffset = 0,
                    LevelName = FoundationLevelName,
                    ElementCode = "W1",
                    Zone = "밸브실",
                    Part = "외벽",
                    StartPoint = new Point3D(0, 0, 0),
                    EndPoint = new Point3D(size.Length, 0, 0),
                    IsExterior = true,
                }
            };
        }

        public static IReadOnlyList<ProfileWallDefinition> CalculateProfileWalls(PumpCreationRequestDto dto)
        {
            var pr = dto.ProfileSpecDto;
            var size = GetSampleSize(dto);
            var height = GetValveRoomHeight(dto);

            return new List<ProfileWallDefinition>
            {
                new ProfileWallDefinition
                {
                    Thickness = pr.T4,
                    LevelName = FoundationLevelName,
                    ElementCode = "PW1",
                    Zone = "밸브실",
                    Part = "프로파일벽 샘플",
                    IsExterior = true,
                    Points = new List<Point3D>
                    {
                        new Point3D(0, size.Width, 0),
                        new Point3D(size.Length, size.Width, 0),
                        new Point3D(size.Length, size.Width, height),
                        new Point3D(0, size.Width, height),
                    },
                }
            };
        }

        public static IReadOnlyList<BeamDefinition> CalculateBeams(PumpCreationRequestDto dto)
        {
            var pr = dto.ProfileSpecDto;
            var size = GetSampleSize(dto);
            var z = GetUpperSlabElevation(dto) - pr.T1;

            return new List<BeamDefinition>
            {
                new BeamDefinition
                {
                    Width = pr.GB1,
                    Height = pr.GH1,
                    LevelName = UpperSlabLevelName,
                    ElementCode = "G1",
                    Zone = "밸브실",
                    Part = "보 샘플",
                    StartPoint = new Point3D(0, size.Width / 2, z),
                    EndPoint = new Point3D(size.Length, size.Width / 2, z),
                }
            };
        }

        public static IReadOnlyList<SolidExtrusionDefinition> CalculateSolids(PumpCreationRequestDto dto)
        {
            var pr = dto.ProfileSpecDto;
            var size = GetSampleSize(dto);

            return new List<SolidExtrusionDefinition>
            {
                new SolidExtrusionDefinition
                {
                    Profile = Rectangle3D(0, 0, 0, size.Length, size.Width),
                    Normal = new Vector3D(0, 0, 1),
                    Distance = pr.T2,
                    ElementCode = "DS1",
                    Zone = "밸브실",
                    Part = "버림콘크리트 샘플",
                }
            };
        }

        public static IReadOnlyList<RectangularSlabOpeningDefinition> CalculateRectangularSlabOpenings(PumpCreationRequestDto dto)
        {
            var pr = dto.ProfileSpecDto;
            var size = GetSampleSize(dto);

            return new List<RectangularSlabOpeningDefinition>
            {
                new RectangularSlabOpeningDefinition
                {
                    Width = pr.OB1,
                    Length = pr.OB1,
                    Position = new Point2D(size.Length / 2, size.Width / 2),
                    LevelName = UpperSlabLevelName,
                    HostElementCode = "S1",
                    ElementCode = "SO1",
                    Zone = "밸브실",
                    Part = "슬래브 오프닝 샘플",
                }
            };
        }

        public static IReadOnlyList<CircularSlabOpeningDefinition> CalculateCircularSlabOpenings(PumpCreationRequestDto dto)
        {
            var d = dto.DesignConditionDto;
            var size = GetSampleSize(dto);

            return new List<CircularSlabOpeningDefinition>
            {
                new CircularSlabOpeningDefinition
                {
                    Diameter = d.D,
                    Position = new Point2D(size.Length / 2, size.Width / 2),
                    LevelName = UpperSlabLevelName,
                    HostElementCode = "S1",
                    ElementCode = "SO2",
                    Zone = "밸브실",
                    Part = "원형 슬래브 오프닝 샘플",
                }
            };
        }

        public static IReadOnlyList<RectangularWallOpeningDefinition> CalculateRectangularWallOpenings(PumpCreationRequestDto dto)
        {
            var pr = dto.ProfileSpecDto;
            var size = GetSampleSize(dto);

            return new List<RectangularWallOpeningDefinition>
            {
                new RectangularWallOpeningDefinition
                {
                    Width = pr.OB1,
                    Height = pr.OH1,
                    Position = new Point3D(size.Length / 2, 0, 0),
                    LevelName = FoundationLevelName,
                    HostElementCode = "W1",
                    OffsetZ = pr.H6,
                    ElementCode = "WO1",
                    Zone = "밸브실",
                    Part = "벽 오프닝 샘플",
                }
            };
        }

        public static IReadOnlyList<CircularWallOpeningDefinition> CalculateCircularWallOpenings(PumpCreationRequestDto dto)
        {
            var d = dto.DesignConditionDto;
            var pr = dto.ProfileSpecDto;
            var size = GetSampleSize(dto);

            return new List<CircularWallOpeningDefinition>
            {
                new CircularWallOpeningDefinition
                {
                    Diameter = d.D,
                    Position = new Point3D(size.Length / 2, 0, 0),
                    LevelName = FoundationLevelName,
                    HostElementCode = "W1",
                    OffsetZ = pr.H6,
                    ElementCode = "WO2",
                    Zone = "밸브실",
                    Part = "원형 벽 오프닝 샘플",
                }
            };
        }

        public static IReadOnlyList<GenericModelPlacementDefinition> CalculateGenericModels(PumpCreationRequestDto dto)
        {
            var pr = dto.ProfileSpecDto;
            var size = GetSampleSize(dto);
            var valveBase = dto.ValveBase;

            return new List<GenericModelPlacementDefinition>
            {
                new GenericModelPlacementDefinition
                {
                    SymbolName = "DH_받침",
                    Origin = new Point3D(size.Length / 2, size.Width / 2, 0),
                    LevelName = FoundationLevelName,
                    Rotation = -90,
                    ElementCode = "PED1",
                    Zone = "밸브실",
                    Part = "콘크리트기초 샘플",
                    Parameters = new Dictionary<string, object>
                    {
                        { "B", valveBase.ValveBaseWidth },
                        { "L", valveBase.ValveBaseLength },
                        { "H", valveBase.ValveBaseHeight },
                        { "T", pr.T3 },
                    },
                }
            };
        }

        public static IReadOnlyList<SectionViewDefinition> CalculateSectionViews(PumpCreationRequestDto dto)
        {
            var size = GetSampleSize(dto);
            var minZ = GetValveRoomElevation(dto) - 500;
            var maxZ = GetUpperSlabElevation(dto) + 500;

            return new List<SectionViewDefinition>
            {
                new SectionViewDefinition
                {
                    Name = "A",
                    Min = new Point3D(-500, size.Width / 2, minZ),
                    Max = new Point3D(size.Length + 500, size.Width / 2 + 500, maxZ),
                    BasisX = new Vector3D(1, 0, 0),
                    BasisZ = new Vector3D(0, 1, 0),
                }
            };
        }

        public static IReadOnlyList<StairsDefinition> CalculateStairs(PumpCreationRequestDto dto)
        {
            var pr = dto.ProfileSpecDto;
            var size = GetSampleSize(dto);
            var baseZ = GetValveRoomElevation(dto);
            var risers = Math.Max(1, pr.NS1);
            var treadDepth = pr.HS1 > 0 ? 300 : 300;
            var runLength = Math.Max(300, (risers - 1) * treadDepth);

            return new List<StairsDefinition>
            {
                new StairsDefinition
                {
                    BaseLevelName = FoundationLevelName,
                    TopLevelName = UpperSlabLevelName,
                    TypeName = "현장타설",
                    ElementCode = "ST1",
                    Zone = "밸브실",
                    Part = "계단 샘플",
                    TreadDepth = treadDepth,
                    MaxRiserHeight = pr.HS1,
                    RisersNumber = risers,
                    Runs = new List<StairsRunDefinition>
                    {
                        new StairsRunDefinition
                        {
                            StartPoint = new Point3D(size.Length - 500, size.Width / 2, baseZ),
                            EndPoint = new Point3D(size.Length - 500 - runLength, size.Width / 2, baseZ),
                            Justification = StairJustification.Center,
                            Width = 800,
                        }
                    },
                }
            };
        }

        private static (double Length, double Width) GetSampleSize(PumpCreationRequestDto dto)
        {
            var d = dto.DesignConditionDto;
            var pr = dto.ProfileSpecDto;
            var pl = dto.PlanSpecDto;
            var length = pr.B7 + pr.T4 * 2;
            var width = pl.B8 + pr.T4 * 2;

            return (Math.Max(length, 3000), Math.Max(width, 2000));
        }

        private static double GetUpperSlabElevation(PumpCreationRequestDto dto)
        {
            var d = dto.DesignConditionDto;
            var pr = dto.ProfileSpecDto;
            return d.HWL * 1000 + pr.H3;
        }

        private static double GetValveRoomElevation(PumpCreationRequestDto dto)
        {
            var d = dto.DesignConditionDto;
            var pr = dto.ProfileSpecDto;
            return GetUpperSlabElevation(dto) - pr.H7 - d.D - pr.H6;
        }

        private static double GetValveRoomHeight(PumpCreationRequestDto dto)
        {
            return GetUpperSlabElevation(dto) - GetValveRoomElevation(dto);
        }

        private static IReadOnlyList<Point2D> Rectangle2D(double x, double y, double length, double width)
        {
            return new List<Point2D>
            {
                new Point2D(x, y),
                new Point2D(x + length, y),
                new Point2D(x + length, y + width),
                new Point2D(x, y + width),
            };
        }

        private static IReadOnlyList<Point3D> Rectangle3D(double x, double y, double z, double length, double width)
        {
            return new List<Point3D>
            {
                new Point3D(x, y, z),
                new Point3D(x + length, y, z),
                new Point3D(x + length, y + width, z),
                new Point3D(x, y + width, z),
            };
        }
    }
}