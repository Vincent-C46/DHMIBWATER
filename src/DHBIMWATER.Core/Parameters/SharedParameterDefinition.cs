using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHBIMWATER.Core.Parameters
{
    public enum ParameterSpecType
    {
        Text,
        MultilineText,
        Url,
        YesNo,
        Integer,
        Material,

        Number,
        Length,
        Area,
        Volume,
        Angle,
        Slope,
        Mass,
        Currency,

        Force,
        Moment,
        Stress,
        MassPerUnitLength,
        MassPerUnitArea,
        ReinforcementArea,
        ReinforcementCover,
        ReinforcementSpacing,

        Flow,
        PipeSize,
        PipingPressure,
        PipingVelocity,
    }
    public enum ParameterGroupType
    {
        IdentityData,
        General,
        Data,
        Constraints,
        Phasing,
        Graphics,
        Materials,
        Geometry,
        Length,
        Structural,
        StructuralAnalysis,
        Mechanical,
        MechanicalAirflow,
        MechanicalLoads,
        Electrical,
        ElectricalLighting,
        ElectricalLoads,
        Plumbing,
        EnergyAnalysis,
        Construction,
        FireProtection,
        Ifc,
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
        ProjectInformation,     // OST_ProjectInformation
        Stairs,                 // OST_Stairs

        Views,                  // OST_Views
    }
    public record SharedParameterDefinition
    {
        public required string Name { get; init; }
        public Guid? Guid { get; init; }
        public string GroupName { get; init; } = "DHBIMWATER";
        public required ParameterSpecType SpecType { get; init; }
        public required ParameterGroupType GroupType { get; init; }
        public required ParameterBindingType BindingType { get; init; }
        public required IReadOnlyList<ParameterCategory> Categories { get; init; }
        public bool UserModifiable { get; init; } = true;
    }
}
