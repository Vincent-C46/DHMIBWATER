using DHBIMWATER.Application.DTOs.Revit.Reservoir;
using DHBIMWATER.Application.Services;
using DHBIMWATER.Core.Structures;
using Xunit;

namespace DHBIMWATER.UI.Tests.Services;

public class ReservoirGeometryCalculatorTests
{
    [Fact]
    public void CalculateGenericModels_ReturnsFaultingConcreteAndPitDefinitions()
    {
        var dto = CreateReservoirDto();

        var defs = ReservoirGeometryCalculator.CalculateGenericModels(dto);

        Assert.Equal(2, defs.Count);

        var faulting = defs.Single(d => d.ElementCode == "L3");
        Assert.Equal("DH_단차버림콘크리트", faulting.SymbolName);
        Assert.Equal("Body", faulting.Class);
        Assert.Equal("슬래브", faulting.Category);
        Assert.Equal("배관실", faulting.Zone);
        Assert.Equal("단차_버림콘크리트", faulting.Part);
        Assert.Equal(25175, faulting.Origin.X);
        Assert.Equal(-50, faulting.Origin.Y);
        Assert.Equal(-600, faulting.Origin.Z);
        Assert.Equal(ReservoirGeometryCalculator.TankFoundLevelName, faulting.LevelName);
        Assert.Equal(12325, Assert.IsType<double>(faulting.Parameters["L1"]));
        Assert.Equal(2800, Assert.IsType<double>(faulting.Parameters["L2"]));
        Assert.Equal(5950, Assert.IsType<double>(faulting.Parameters["L3"]));
        Assert.Equal(2100, Assert.IsType<double>(faulting.Parameters["W1"]));
        Assert.Equal(100, Assert.IsType<double>(faulting.Parameters["W2"]));
        Assert.Equal(2000, Assert.IsType<double>(faulting.Parameters["H"]));
        Assert.Equal(new Dimensionless(1.0), Assert.IsType<Dimensionless>(faulting.Parameters["사면비"]));

        var pit = defs.Single(d => d.ElementCode == "P1");
        Assert.Equal("DH_배수지피트", pit.SymbolName);
        Assert.Equal("Body", pit.Class);
        Assert.Equal("PIT", pit.Category);
        Assert.Equal("배관실", pit.Zone);
        Assert.Equal("PIT", pit.Part);
        Assert.Equal(10675, pit.Origin.X);
        Assert.Equal(-4850, pit.Origin.Y);
        Assert.Equal(0, pit.Origin.Z);
        Assert.Equal(ReservoirGeometryCalculator.ValveFoundLevelName, pit.LevelName);
        Assert.Equal(1000, Assert.IsType<double>(pit.Parameters["드레인피트_내부_가로폭"]));
        Assert.Equal(1000, Assert.IsType<double>(pit.Parameters["드레인피트_내부_세로폭"]));
        Assert.Equal(1000, Assert.IsType<double>(pit.Parameters["드레인피트_내부_깊이"]));
        Assert.Equal(300, Assert.IsType<double>(pit.Parameters["드레인피트_벽체두께"]));
        Assert.Equal(300, Assert.IsType<double>(pit.Parameters["드레인피트_바닥두께"]));
        Assert.Equal(100, Assert.IsType<double>(pit.Parameters["드레인피트_버림콘크리트_두께"]));
        Assert.Equal(500, Assert.IsType<double>(pit.Parameters["밸브실_바닥슬라브두께"]));
    }

    [Fact]
    public void CalculateSlabs_UsesValveExteriorWallThicknessForB4AndL4YExtent()
    {
        var dto = CreateReservoirDto();

        var slabs = ReservoirGeometryCalculator.CalculateSlabs(dto);

        var b4 = slabs.Single(s => s.ElementCode == "B4");
        Assert.Equal(-5600, b4.Points.Min(p => p.Y));
        Assert.Equal(0, b4.Points.Max(p => p.Y));

        var l4 = slabs.Single(s => s.ElementCode == "L4");
        Assert.Equal(-5700, l4.Points.Min(p => p.Y));
        Assert.Equal(0, l4.Points.Max(p => p.Y));
    }
    private static ReservoirCreationRequestDto CreateReservoirDto()
    {
        var design = new ReservoirDesignConditionDto(Q: 5000, RT: 12, N: 2, LWL: 0);
        var tank = new ReservoirTankDto(
            He: 4.5,
            Hf: 300,
            Hm: 150,
            W: 25000,
            L: 30000,
            M1: 4000,
            M2: 4000,
            M3: 4000,
            M4: 4000,
            Wh: 2500,
            Lh: 2500,
            Hh: 2000,
            Ltt: 0);
        var valve = new ReservoirValveDto(
            H1F: 4000,
            Lv: 30000,
            Wv: 5000,
            Lvt: 0,
            We: 2500,
            TrOff: 100,
            Wp: 1000,
            Hp: 1000,
            WpThk: 300,
            SpThk: 300,
            SLp: 1000,
            SLv: 1.0);
        var thickness = new ReservoirTypeThicknessDto(
            StuThk: 300,
            StbThk: 500,
            SvuThk: 300,
            SvmThk: 300,
            SvbThk: 500,
            WteThk: 350,
            WtiThk: 350,
            WhThk: 300,
            WveThk: 300,
            WviThk: 300,
            LcThk: 100,
            ColumnTypeName: "Column",
            BeamTypeName: "Beam");

        return new ReservoirCreationRequestDto(design, tank, valve, thickness);
    }
}



