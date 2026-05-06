using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Core.Parameters
{
    public enum ParameterSpecType
    {
        // 문자
        Text,               // SpecTypeId.String.Text
        Url,                // SpecTypeId.String.Url
        MultilineText,      // SpecTypeId.String.MultilineText

        // 숫자
        Integer,            // SpecTypeId.Int.Integer
        Number,             // SpecTypeId.Number
        YesNo,              // SpecTypeId.Boolean.YesNo

        // 치수
        Length,             // SpecTypeId.Length
        Area,               // SpecTypeId.Area
        Volume,             // SpecTypeId.Volume
        Angle,              // SpecTypeId.Angle
        Slope,              // SpecTypeId.Slope

        // 구조
        Force,              // SpecTypeId.Force
        Stress,             // SpecTypeId.Stress
        Mass,               // SpecTypeId.Mass
        MassPerUnitLength,  // SpecTypeId.MassPerUnitLength

        // 기계
        Flow,               // SpecTypeId.Flow
        Pressure,           // SpecTypeId.Pressure
        Temperature,        // SpecTypeId.HvacTemperature
        Velocity,           // SpecTypeId.HvacVelocity

        // 전기
        ElectricalPower,    // SpecTypeId.ElectricalPower
        ElectricalCurrent,  // SpecTypeId.ElectricalCurrent
        ElectricalVoltage,  // SpecTypeId.ElectricalVoltage

        // 기타
        Currency,           // SpecTypeId.Currency
        TimeInterval,       // SpecTypeId.TimeInterval
    }
    public enum ParameterGroupType
    {
        // 일반
        General,            // GroupTypeId.General
        Data,               // GroupTypeId.Data

        // 식별
        IdentityData,       // GroupTypeId.IdentityData

        // 치수/기하
        Geometry,           // GroupTypeId.Geometry

        // 구조
        Structural,         // GroupTypeId.Structural
        StructuralAnalysis, // GroupTypeId.StructuralAnalysis

        // 기계/설비
        Mechanical,         // GroupTypeId.Mechanical
        MechanicalAirflow,  // GroupTypeId.MechanicalAirflow
        MechanicalLoads,    // GroupTypeId.MechanicalLoads

        // 전기
        Electrical,         // GroupTypeId.Electrical
        ElectricalLoads,    // GroupTypeId.ElectricalLoads
        ElectricalLighting, // GroupTypeId.ElectricalLighting

        // 배관
        Plumbing,           // GroupTypeId.Plumbing

        // 에너지
        EnergyAnalysis,     // GroupTypeId.EnergyAnalysis

        // 기타
        Construction,       // GroupTypeId.Construction
        Graphics,           // GroupTypeId.Graphics
        Materials,          // GroupTypeId.Materials
        Phases,             // GroupTypeId.Phases
        Constraints,        // GroupTypeId.Constraints
        Analysis,           // GroupTypeId.Analysis
        FireProtection,     // GroupTypeId.FireProtection
    }
    public enum ParameterBindingType
    {
        Instance,
        Type
    }
    public enum ParameterCategory
    {
        GenericModel,           // OST_GenericModel
        Walls,                  // OST_Walls
        Floors,                 // OST_Floors
        StructuralColumns,      // OST_StructuralColumns
        StructuralFraming,      // OST_StructuralFraming
        StructuralFoundation,   // OST_StructuralFoundation
        Doors,                  // OST_Doors
        Windows,                // OST_Windows
        ProjectInformation,     // OST_ProjectInformation
        Stairs,                 // OST_Stairs
        Ramps,                  // OST_Ramps
    }
    public record SharedParameterDefinition
    {
        public required string Name { get; init; }
        public required Guid Guid { get; init; }
        public required string GroupName { get; init; }
        public required ParameterSpecType SpecType { get; init; }
        public required ParameterGroupType GroupType { get; init; }
        public required ParameterBindingType BindingType { get; init; }
        public required IReadOnlyList<ParameterCategory> Categories { get; init; }
        public bool UserModifiable { get; init; } = true;
    }
}
