using DHBIMWATER.Application.DTOs.Revit.Reservoir;
using DHBIMWATER.Core.Geometry;
using DHBIMWATER.Core.Structures;
using System;
using System.Collections.Generic;

namespace DHBIMWATER.Application.Services
{
    /// <summary>
    /// 배수지(저수조) 지오메트리 계산기.
    /// 모든 좌표 단위는 mm.
    /// DTO 단위: LWL(m)·He(m) → *1000 변환, 나머지 치수는 이미 mm.
    /// 원점(0,0): 수조부 내부 좌하단.
    /// </summary>
    public static class ReservoirGeometryCalculator
    {
        #region Level Names
        public const string TankFoundLevelName  = "수조부 바닥슬래브";
        public const string TankUpperLevelName  = "수조부 상부슬래브";
        public const string ValveFoundLevelName = "밸브실 바닥슬래브";
        public const string ValveMidLevelName   = "밸브실 중간슬래브";
        public const string LWLLevelName        = "LWL";
        public const string HWLLevelName        = "HWL";
        #endregion

        // ─────────────────────────────────────────────────────────── Public API

        public static IReadOnlyList<LevelDefinition> CalculateLevels(ReservoirCreationRequestDto dto)
        {
            var (tfE, tuE, vfE, vmE) = LevelElevations(dto);
            double lwlE = dto.DesignConditionDto.LWL * 1000;
            double hwlE = lwlE + dto.TankDto.He * 1000;
            return new List<LevelDefinition>
            {
                new LevelDefinition { Name = TankFoundLevelName,  Elevation = tfE  },
                new LevelDefinition { Name = TankUpperLevelName,  Elevation = tuE  },
                new LevelDefinition { Name = ValveFoundLevelName, Elevation = vfE  },
                new LevelDefinition { Name = ValveMidLevelName,   Elevation = vmE  },
                new LevelDefinition { Name = LWLLevelName,        Elevation = lwlE },
                new LevelDefinition { Name = HWLLevelName,        Elevation = hwlE },
            };
        }

        public static IReadOnlyList<SlabDefinition> CalculateSlabs(ReservoirCreationRequestDto dto)
        {
            var d  = dto.DesignConditionDto;
            var t  = dto.TankDto;
            var v  = dto.ValveDto;
            var th = dto.ThicknessDto;

            int    n     = d.N;
            double w     = t.W;       // mm
            double l     = t.L;       // mm
            double ltt   = t.Ltt;     // mm
            double wh    = t.Wh;      // mm
            double lh    = t.Lh;      // mm
            double lv    = v.Lv;      // mm
            double wv    = v.Wv;      // mm
            double lvt   = v.Lvt;     // mm
            double we    = v.We;      // mm
            double trOff = v.TrOff;   // mm

            double stuThk = th.StuThk;
            double stbThk = th.StbThk;
            double wteThk = th.WteThk;
            double wtiThk = th.WtiThk;
            double whThk  = th.WhThk;
            double svuThk = th.SvuThk;
            double svmThk = th.SvmThk;
            double svbThk = th.SvbThk;
            double wveThk = th.WveThk;
            double wviThk = th.WviThk;
            double lcThk  = th.LcThk;

            var (tfE, tuE, vfE, vmE) = LevelElevations(dto);

            double innerWallCX = w + wtiThk / 2;

            var slabs = new List<SlabDefinition>();

            // ──────── B1/L1: 수조부 기초 (N셀 loop)
            double s_W = w + wtiThk / 2 + wteThk + ltt;
            double s_L = l + 2 * wteThk + 2 * ltt;
            double bp1 = lv / 2 + wveThk - wh - wtiThk / 2;
            double bp2 = wh + wtiThk / 2;
            double bl1 = ltt;
            double bl2 = lh + wteThk;

            for (int i = 0; i < n; i++)
            {
                bool isFirst = (i == 0);
                bool isLast  = (i == n - 1);
                double xOff  = i * (w + wtiThk);

                List<Point2D> b1Pts;
                if      (isFirst && n >= 2) b1Pts = LeftCellFoundShape(-(wteThk + ltt), -(wteThk + ltt), s_W, s_L, bp1, bp2, bl1, bl2);
                else if (isLast  && n >= 2) b1Pts = RightCellFoundShape(xOff - wtiThk / 2, -(wteThk + ltt), s_W, s_L, bp1, bp2, bl1, bl2);
                else                         b1Pts = Rect(xOff - wtiThk / 2, -(wteThk + ltt), w + wtiThk, s_L);

                slabs.Add(new SlabDefinition
                {
                    Points = b1Pts, SubPoints = Array.Empty<Point2D>(),
                    Thickness = stbThk, ElevationZ = tfE, LevelName = TankFoundLevelName,
                    ElementCode = "B1", Zone = "수조부", Part = "기초콘크리트",
                });

                List<Point2D> l1Pts;
                if      (isFirst && n >= 2) l1Pts = LeftCellFoundShape(-(wteThk + ltt + lcThk), -(wteThk + ltt + lcThk), s_W + lcThk, s_L + 2 * lcThk, bp1 - whThk, bp2 + whThk, bl1 + lcThk + wteThk, lh + wteThk);
                else if (isLast  && n >= 2) l1Pts = RightCellFoundShape(xOff - wtiThk / 2, -(wteThk + ltt + lcThk), s_W + lcThk, s_L + 2 * lcThk, bp1 - whThk, bp2 + whThk, bl1 + lcThk + wteThk, lh + wteThk);
                else                         l1Pts = Rect(xOff - wtiThk / 2, -(wteThk + ltt + lcThk), w + wtiThk, s_L + 2 * lcThk);

                slabs.Add(new SlabDefinition
                {
                    Points = l1Pts, SubPoints = Array.Empty<Point2D>(),
                    Thickness = lcThk, ElevationZ = tfE - stbThk - lcThk, LevelName = ValveFoundLevelName,
                    ElementCode = "L1", Zone = "수조부", Part = "버림콘크리트",
                });

                double s1StartX = (n == 1 || isFirst) ? -wteThk : xOff - wtiThk / 2;
                double s1W = (n == 1) ? w + 2 * wteThk
                           : isFirst  ? w + wteThk + wtiThk / 2
                           : isLast   ? w + wteThk + wtiThk / 2
                           :            w + wtiThk;
                slabs.Add(new SlabDefinition
                {
                    Points = Rect(s1StartX, -wteThk, s1W, l + 2 * wteThk), SubPoints = Array.Empty<Point2D>(),
                    Thickness = stuThk, ElevationZ = tuE, LevelName = TankUpperLevelName,
                    ElementCode = "S1", Zone = "수조부", Part = "상부슬래브",
                });
            }

            // ──────── B2/L2: 배관실 호퍼 기초
            double b2StartX = w - wh - whThk;
            double b2StartY = whThk - wteThk;
            double b2W      = 2 * wh + wtiThk + 2 * whThk;
            double b2L      = lh + wteThk;

            slabs.Add(new SlabDefinition
            {
                Points = Rect(b2StartX, b2StartY, b2W, b2L), SubPoints = Array.Empty<Point2D>(),
                Thickness = svbThk, ElevationZ = vfE, LevelName = ValveFoundLevelName,
                ElementCode = "B2", Zone = "배관실", Part = "기초콘크리트",
            });
            slabs.Add(new SlabDefinition
            {
                Points = Rect(b2StartX, b2StartY, b2W, b2L), SubPoints = Array.Empty<Point2D>(),
                Thickness = lcThk, ElevationZ = vfE - svbThk - lcThk, LevelName = ValveFoundLevelName,
                ElementCode = "L2", Zone = "배관실", Part = "버림콘크리트",
            });

            // ──────── B4/L4: 배관실 외측 기초
            double vCenterX = innerWallCX;
            double b4StartX = vCenterX - lv / 2 - wveThk - lvt;
            double b4StartY = -(wveThk + wteThk + wv + lvt);
            double b4W      = 2 * lvt + lv + 2 * wveThk;
            double b4L      = wveThk + wteThk + wv + lvt;

            slabs.Add(new SlabDefinition
            {
                Points = Rect(b4StartX, b4StartY, b4W, b4L), SubPoints = Array.Empty<Point2D>(),
                Thickness = svbThk, ElevationZ = vfE, LevelName = ValveFoundLevelName,
                ElementCode = "B4", Zone = "배관실", Part = "기초콘크리트",
            });
            slabs.Add(new SlabDefinition
            {
                Points = Rect(b4StartX - lcThk, b4StartY - lcThk, b4W + 2 * lcThk, b4L + lcThk), SubPoints = Array.Empty<Point2D>(),
                Thickness = lcThk, ElevationZ = vfE - svbThk - lcThk, LevelName = ValveFoundLevelName,
                ElementCode = "L4", Zone = "배관실", Part = "버림콘크리트",
            });

            // ──────── MS1: 배관실 중간슬래브
            double vmStartX = vCenterX - lv / 2;
            double vmStartY = -(wteThk + wv);

            slabs.Add(new SlabDefinition
            {
                Points = Rect(vmStartX, vmStartY, lv, wv), SubPoints = Array.Empty<Point2D>(),
                Thickness = svmThk, ElevationZ = vmE, LevelName = ValveMidLevelName,
                ElementCode = "MS1", Zone = "배관실", Part = "중간슬래브",
            });

            // ──────── S2: 배관실 상부슬래브
            slabs.Add(new SlabDefinition
            {
                Points = Rect(vmStartX - wveThk, vmStartY - wveThk, lv + 2 * wveThk, wv + wveThk), SubPoints = Array.Empty<Point2D>(),
                Thickness = svuThk, ElevationZ = tuE, LevelName = TankUpperLevelName,
                ElementCode = "S2", Zone = "배관실", Part = "상부슬래브",
            });

            // ──────── TC1/TC2/TC3: 덧침콘크리트
            double tcStartX = vmStartX + trOff;
            double tcStartY = vmStartY + trOff;

            slabs.Add(new SlabDefinition
            {
                Points = Rect(tcStartX, tcStartY, lv - 2 * trOff, wv - 2 * trOff), SubPoints = Array.Empty<Point2D>(),
                Thickness = 50, ElevationZ = vfE, LevelName = ValveFoundLevelName,
                ElementCode = "TC1", Zone = "배관실", Part = "바닥슬래브_덧침콘크리트",
            });
            slabs.Add(new SlabDefinition
            {
                Points = Rect(tcStartX, tcStartY, lv - we - 2 * trOff - wviThk, wv - 2 * trOff), SubPoints = Array.Empty<Point2D>(),
                Thickness = 50, ElevationZ = vmE, LevelName = ValveMidLevelName,
                ElementCode = "TC2", Zone = "배관실", Part = "중간슬래브_덧침콘크리트",
            });
            double tc3StartX = vmStartX + (lv - we + trOff);
            slabs.Add(new SlabDefinition
            {
                Points = Rect(tc3StartX, tcStartY, we - 2 * trOff, wv - 2 * trOff), SubPoints = Array.Empty<Point2D>(),
                Thickness = 50, ElevationZ = vmE, LevelName = ValveMidLevelName,
                ElementCode = "TC3", Zone = "배관실", Part = "전기실_덧침콘크리트",
            });

            return slabs;
        }

        public static IReadOnlyList<LinearWallDefinition> CalculateLinearWalls(ReservoirCreationRequestDto dto)
        {
            var d  = dto.DesignConditionDto;
            var t  = dto.TankDto;
            var v  = dto.ValveDto;
            var th = dto.ThicknessDto;

            int    n  = d.N;
            double w  = t.W;   // mm
            double l  = t.L;   // mm
            double wh = t.Wh;  // mm
            double lh = t.Lh;  // mm
            double lv = v.Lv;  // mm
            double wv = v.Wv;  // mm
            double we = v.We;  // mm

            double wteThk = th.WteThk;
            double wtiThk = th.WtiThk;
            double whThk  = th.WhThk;
            double wveThk = th.WveThk;
            double wviThk = th.WviThk;

            var (tfE, tuE, vfE, vmE) = LevelElevations(dto);

            double totalIW  = n * w + (n - 1) * wtiThk;
            double innerWCX = w + wtiThk / 2;
            double vCenterX = innerWCX;

            double tankWallH  = tuE - tfE;
            double hopperH    = tfE - vfE;
            double valveFullH = tuE - vfE;
            double elecWallH  = tuE - vmE;

            var walls = new List<LinearWallDefinition>();

            walls.Add(Wall("W1", wteThk, TankFoundLevelName, tankWallH,
                new Point3D(-wteThk / 2, 0, 0), new Point3D(-wteThk / 2, l, 0), "수조부", "외벽_좌측"));

            double w2X = totalIW + wteThk / 2;
            walls.Add(Wall("W2", wteThk, TankFoundLevelName, tankWallH,
                new Point3D(w2X, 0, 0), new Point3D(w2X, l, 0), "수조부", "외벽_우측"));

            walls.Add(Wall("W3", wteThk, TankFoundLevelName, tankWallH,
                new Point3D(-wteThk, l + wteThk / 2, 0), new Point3D(totalIW + wteThk, l + wteThk / 2, 0), "수조부", "외벽_후면"));

            double w4_1EndX = vCenterX - lv / 2 - wveThk;
            double w4_2StaX = vCenterX + lv / 2 + wveThk;
            walls.Add(Wall("W4", wteThk, TankFoundLevelName, tankWallH,
                new Point3D(-wteThk, -wteThk / 2, 0), new Point3D(w4_1EndX, -wteThk / 2, 0), "수조부", "외벽_전면"));
            walls.Add(Wall("W4", wteThk, TankFoundLevelName, tankWallH,
                new Point3D(w4_2StaX, -wteThk / 2, 0), new Point3D(totalIW + wteThk, -wteThk / 2, 0), "수조부", "외벽_전면"));

            double w5_1EndX = w - wh;
            double w5_2EndX = w + wh + wtiThk;
            walls.Add(Wall("W5", wteThk, TankFoundLevelName, tankWallH,
                new Point3D(w4_1EndX, -wteThk / 2, 0), new Point3D(w5_1EndX, -wteThk / 2, 0), "배관실", "경계부_벽체"));
            walls.Add(Wall("W5", wtiThk, TankFoundLevelName, tankWallH,
                new Point3D(w5_1EndX, -wteThk / 2, 0), new Point3D(w5_2EndX, -wteThk / 2, 0), "배관실", "경계부_벽체"));
            walls.Add(Wall("W5", wteThk, TankFoundLevelName, tankWallH,
                new Point3D(w5_2EndX, -wteThk / 2, 0), new Point3D(w4_2StaX, -wteThk / 2, 0), "배관실", "경계부_벽체"));

            for (int i = 0; i < n - 1; i++)
            {
                double wxCenter = (i + 1) * (w + wtiThk) - wtiThk / 2;
                if (i == 0)
                {
                    walls.Add(Wall("W6", wtiThk, TankFoundLevelName, tankWallH,
                        new Point3D(wxCenter, lh, 0), new Point3D(wxCenter, l, 0), "수조부", "내벽"));
                    walls.Add(Wall("W6", wtiThk, TankFoundLevelName, tankWallH,
                        new Point3D(wxCenter, 0, 0), new Point3D(wxCenter, -lh, 0), "수조부", "내벽"));
                }
                else
                {
                    walls.Add(Wall("W6", wtiThk, TankFoundLevelName, tankWallH,
                        new Point3D(wxCenter, 0, 0), new Point3D(wxCenter, l, 0), "수조부", "내벽"));
                }
            }

            double b3_1Sy = -wteThk + whThk / 2;
            walls.Add(Wall("B3", whThk, ValveFoundLevelName, hopperH,
                new Point3D(w4_1EndX, b3_1Sy, 0), new Point3D(w5_1EndX, b3_1Sy, 0), "배관실", "Hopper_벽체"));
            double b3_2X  = w5_1EndX - whThk / 2;
            double b3_2Sy = -wteThk + whThk;
            walls.Add(Wall("B3", whThk, ValveFoundLevelName, hopperH,
                new Point3D(b3_2X, b3_2Sy, 0), new Point3D(b3_2X, lh, 0), "배관실", "Hopper_벽체"));
            double b3_4X = w5_2EndX + whThk / 2;
            walls.Add(Wall("B3", whThk, ValveFoundLevelName, hopperH,
                new Point3D(b3_4X, b3_2Sy, 0), new Point3D(b3_4X, lh, 0), "배관실", "Hopper_벽체"));
            walls.Add(Wall("B3", whThk, ValveFoundLevelName, hopperH,
                new Point3D(b3_2X - whThk / 2, lh + whThk / 2, 0), new Point3D(b3_4X + whThk / 2, lh + whThk / 2, 0), "배관실", "Hopper_벽체"));
            walls.Add(Wall("B3", whThk, ValveFoundLevelName, hopperH,
                new Point3D(w5_2EndX, b3_1Sy, 0), new Point3D(w4_2StaX, b3_1Sy, 0), "배관실", "Hopper_벽체"));

            double w7X     = vCenterX - lv / 2 - wveThk / 2;
            double w79TopY = -wteThk;
            double w79BotY = -(wteThk + wv);
            walls.Add(Wall("W7", wveThk, ValveFoundLevelName, valveFullH,
                new Point3D(w7X, w79TopY, 0), new Point3D(w7X, w79BotY, 0), "배관실", "외벽_좌측"));
            double w8X = vCenterX + lv / 2 + wveThk / 2;
            walls.Add(Wall("W8", wveThk, ValveFoundLevelName, valveFullH,
                new Point3D(w8X, w79TopY, 0), new Point3D(w8X, w79BotY, 0), "배관실", "외벽_우측"));
            walls.Add(Wall("W9", wveThk, ValveFoundLevelName, valveFullH,
                new Point3D(w7X - wveThk / 2, w79BotY - wveThk / 2, 0),
                new Point3D(w8X + wveThk / 2, w79BotY - wveThk / 2, 0), "배관실", "외벽_전면"));

            double w10X = w8X - wveThk / 2 - we - wviThk / 2;
            walls.Add(Wall("W10", wviThk, ValveMidLevelName, elecWallH,
                new Point3D(w10X, w79TopY, 0), new Point3D(w10X, w79BotY, 0), "배관실", "전기실_벽체"));

            return walls;
        }

        public static IReadOnlyList<ColumnDefinition> CalculateColumns(ReservoirCreationRequestDto dto)
        {
            var d  = dto.DesignConditionDto;
            var t  = dto.TankDto;
            var th = dto.ThicknessDto;

            int    n  = d.N;
            double w  = t.W;   // mm
            double l  = t.L;   // mm
            double m1 = t.M1;  // mm
            double m2 = t.M2;  // mm
            double m3 = t.M3;  // mm
            double m4 = t.M4;  // mm
            double wtiThk = th.WtiThk;

            var (tfE, tuE, _, _) = LevelElevations(dto);
            double colH = tuE - tfE;

            const double maxCTC = 5000;
            double colSpan  = w - m3 - m4;
            double rowSpan  = l - m1 - m2;
            int    colNum   = (colSpan % maxCTC == 0) ? (int)(colSpan / maxCTC) + 1 : (int)(colSpan / maxCTC) + 2;
            int    rowNum   = (rowSpan % maxCTC == 0) ? (int)(rowSpan / maxCTC) + 1 : (int)(rowSpan / maxCTC) + 2;
            double colOff   = Math.Round(colSpan / (colNum - 1), 0);
            double rowOff   = Math.Round(rowSpan / (rowNum - 1), 0);

            var columns = new List<ColumnDefinition>();
            for (int ci = 0; ci < n; ci++)
            {
                double xOff = ci * (w + wtiThk);
                for (int r = 0; r < rowNum; r++)
                    for (int c = 0; c < colNum; c++)
                        columns.Add(new ColumnDefinition
                        {
                            Position    = new Point3D(xOff + m3 + c * colOff, m2 + r * rowOff, 0),
                            TypeName    = th.ColumnTypeName,
                            LevelName   = TankFoundLevelName,
                            Height      = colH,
                            ElementCode = "C1",
                            Zone        = "수조부",
                            Part        = "기둥",
                        });
            }
            return columns;
        }

        public static IReadOnlyList<BeamDefinition> CalculateBeams(ReservoirCreationRequestDto dto)
        {
            var d  = dto.DesignConditionDto;
            var t  = dto.TankDto;
            var th = dto.ThicknessDto;

            int    n  = d.N;
            double w  = t.W;   // mm
            double l  = t.L;   // mm
            double m1 = t.M1;  // mm
            double m2 = t.M2;  // mm
            double m3 = t.M3;  // mm
            double m4 = t.M4;  // mm
            double lh = t.Lh;  // mm
            double wh = t.Wh;  // mm
            double wtiThk = th.WtiThk;

            var (tfE, tuE, _, _) = LevelElevations(dto);
            double beamZ = tuE;

            const double maxCTC = 5000;
            double colSpan = w - m3 - m4;
            double rowSpan = l - m1 - m2;
            int    colNum  = (colSpan % maxCTC == 0) ? (int)(colSpan / maxCTC) + 1 : (int)(colSpan / maxCTC) + 2;
            int    rowNum  = (rowSpan % maxCTC == 0) ? (int)(rowSpan / maxCTC) + 1 : (int)(rowSpan / maxCTC) + 2;
            double colOff  = Math.Round(colSpan / (colNum - 1), 0);
            double rowOff  = Math.Round(rowSpan / (rowNum - 1), 0);

            var beams = new List<BeamDefinition>();

            for (int ci = 0; ci < n; ci++)
            {
                double xOff = ci * (w + wtiThk);
                var pts = ColumnGrid(xOff + m3, m2, rowOff, colOff, rowNum, colNum, beamZ);

                string beamType = th.BeamTypeName;
                for (int i = 0; i < pts.Count; i++)
                {
                    if ((i + 1) % colNum != 0)
                        beams.Add(Beam("G1", beamType, TankUpperLevelName,
                            new Point3D(pts[i].X, pts[i].Y, beamZ),
                            new Point3D(pts[i + 1].X, pts[i + 1].Y, beamZ), "수조부", "보"));

                    if (i + colNum < pts.Count)
                        beams.Add(Beam("G1", beamType, TankUpperLevelName,
                            new Point3D(pts[i].X, pts[i].Y, beamZ),
                            new Point3D(pts[i + colNum].X, pts[i + colNum].Y, beamZ), "수조부", "보"));

                    if (i >= colNum * (rowNum - 1))
                        beams.Add(Beam("G1", beamType, TankUpperLevelName,
                            new Point3D(pts[i].X, pts[i].Y, beamZ),
                            new Point3D(pts[i].X, pts[i].Y + m1, beamZ), "수조부", "보"));

                    if (i < colNum)
                        beams.Add(Beam("G1", beamType, TankUpperLevelName,
                            new Point3D(pts[i].X, pts[i].Y, beamZ),
                            new Point3D(pts[i].X, pts[i].Y - m2, beamZ), "수조부", "보"));

                    if (i % colNum == 0)
                        beams.Add(Beam("G1", beamType, TankUpperLevelName,
                            new Point3D(pts[i].X, pts[i].Y, beamZ),
                            new Point3D(pts[i].X - m3, pts[i].Y, beamZ), "수조부", "보"));

                    if (i % colNum == colNum - 1)
                        beams.Add(Beam("G1", beamType, TankUpperLevelName,
                            new Point3D(pts[i].X, pts[i].Y, beamZ),
                            new Point3D(pts[i].X + m4, pts[i].Y, beamZ), "수조부", "보"));
                }

                // H2: 기초 헌치
                var fndPts = new List<Point3D>
                {
                    new Point3D(xOff + w, lh, tfE),
                    new Point3D(xOff + w, l,  tfE),
                    new Point3D(xOff,     l,  tfE),
                    new Point3D(xOff,     0,  tfE),
                    new Point3D(xOff + w - wh, 0, tfE),
                };
                for (int i = 0; i < fndPts.Count - 1; i++)
                    beams.Add(Beam("H2", beamType, TankFoundLevelName, fndPts[i], fndPts[i + 1], "수조부", "HAUNCH"));

                // H1: 상부 헌치
                var upperPts = BuildUpperHaunchPoints(xOff, w, l, m1, m2, m3, m4, rowOff, colOff, rowNum, colNum, beamZ);
                for (int i = 0; i < upperPts.Count - 1; i++)
                    beams.Add(Beam("H1", beamType, TankUpperLevelName, upperPts[i], upperPts[i + 1], "수조부", "HAUNCH"));
            }

            return beams;
        }

        // ─────────────────────────────────────────────── Private Helpers

        /// <summary>레벨 절대 표고 계산 (mm). LWL·He는 m 단위 DTO이므로 *1000 변환.</summary>
        private static (double tf, double tu, double vf, double vm) LevelElevations(ReservoirCreationRequestDto dto)
        {
            var d  = dto.DesignConditionDto;
            var t  = dto.TankDto;
            var v  = dto.ValveDto;
            var th = dto.ThicknessDto;

            double lwl = d.LWL * 1000;  // m → mm
            double he  = t.He  * 1000;  // m → mm
            double hm  = t.Hm;          // 이미 mm
            double hf  = t.Hf;          // 이미 mm
            double hh  = t.Hh;          // 이미 mm
            double h1f = v.H1F;         // 이미 mm

            double tf = lwl - hm;
            double tu = tf + hm + he + hf + th.StuThk;
            double vf = tf - hh;
            double vm = vf + h1f + th.SvmThk;
            return (tf, tu, vf, vm);
        }

        private static List<Point2D> LeftCellFoundShape(
            double sx, double sy, double W, double L, double w1, double w2, double l1, double l2)
        {
            if (l1 > 0)
                return new List<Point2D>
                {
                    new Point2D(sx,               sy),
                    new Point2D(sx + W - (w1+w2), sy),
                    new Point2D(sx + W - (w1+w2), sy + l1),
                    new Point2D(sx + W - w2,      sy + l1),
                    new Point2D(sx + W - w2,      sy + l1 + l2),
                    new Point2D(sx + W,            sy + l1 + l2),
                    new Point2D(sx + W,            sy + L),
                    new Point2D(sx,                sy + L),
                };
            return new List<Point2D>
            {
                new Point2D(sx,          sy),
                new Point2D(sx + W - w2, sy),
                new Point2D(sx + W - w2, sy + l2),
                new Point2D(sx + W,      sy + l2),
                new Point2D(sx + W,      sy + L),
                new Point2D(sx,          sy + L),
            };
        }

        private static List<Point2D> RightCellFoundShape(
            double sx, double sy, double W, double L, double w1, double w2, double l1, double l2)
        {
            if (l1 > 0)
                return new List<Point2D>
                {
                    new Point2D(sx + w1 + w2, sy),
                    new Point2D(sx + W,        sy),
                    new Point2D(sx + W,        sy + L),
                    new Point2D(sx,            sy + L),
                    new Point2D(sx,            sy + l1 + l2),
                    new Point2D(sx + w2,       sy + l1 + l2),
                    new Point2D(sx + w2,       sy + l1),
                    new Point2D(sx + w1 + w2,  sy + l1),
                };
            return new List<Point2D>
            {
                new Point2D(sx + w2, sy),
                new Point2D(sx + W,  sy),
                new Point2D(sx + W,  sy + L),
                new Point2D(sx,      sy + L),
                new Point2D(sx,      sy + l2),
                new Point2D(sx + w2, sy + l2),
            };
        }

        private static List<Point2D> Rect(double sx, double sy, double W, double L)
            => new List<Point2D>
            {
                new Point2D(sx,     sy),
                new Point2D(sx + W, sy),
                new Point2D(sx + W, sy + L),
                new Point2D(sx,     sy + L),
            };

        private static List<Point3D> ColumnGrid(
            double startX, double startY, double rowOffset, double colOffset,
            int rowNum, int colNum, double z = 0)
        {
            var pts = new List<Point3D>();
            for (int r = 0; r < rowNum; r++)
                for (int c = 0; c < colNum; c++)
                    pts.Add(new Point3D(startX + c * colOffset, startY + r * rowOffset, z));
            return pts;
        }

        private static LinearWallDefinition Wall(
            string code, double thickness, string levelName, double height,
            Point3D start, Point3D end, string zone, string part)
            => new LinearWallDefinition
            {
                ElementCode = code, Thickness = thickness, LevelName = levelName, Height = height,
                StartPoint = start, EndPoint = end, Zone = zone, Part = part,
            };

        private static BeamDefinition Beam(
            string code, string typeName, string levelName,
            Point3D start, Point3D end, string zone, string part)
            => new BeamDefinition
            {
                ElementCode = code, TypeName = typeName, LevelName = levelName,
                StartPoint = start, EndPoint = end, Zone = zone, Part = part,
                ZJustification = 0,
            };

        private static List<Point3D> BuildUpperHaunchPoints(
            double xOff, double w, double l,
            double m1, double m2, double m3, double m4,
            double rowOff, double colOff, int rowNum, int colNum,
            double z)
        {
            var pts = new List<Point3D>();
            pts.Add(new Point3D(xOff, 0, z));
            for (int r = 0; r < rowNum; r++) pts.Add(new Point3D(xOff, m2 + r * rowOff, z));
            pts.Add(new Point3D(xOff, l, z));
            for (int c = 0; c < colNum; c++) pts.Add(new Point3D(xOff + m3 + c * colOff, l, z));
            pts.Add(new Point3D(xOff + w, l, z));
            for (int r = 0; r < rowNum; r++) pts.Add(new Point3D(xOff + w, l - m1 - r * rowOff, z));
            pts.Add(new Point3D(xOff + w, 0, z));
            for (int c = 0; c < colNum; c++) pts.Add(new Point3D(xOff + w - m4 - c * colOff, 0, z));
            return pts;
        }
    }
}
