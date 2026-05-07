using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using DHBIMWATER.Core.Parameters;
using App = Autodesk.Revit.ApplicationServices.Application;

namespace DHBIMWATER.Infrastructure.Converters
{
    public static class RevitParameterMapper
    {
        public static ForgeTypeId ToSpecTypeId(ParameterSpecType specType)
            => specType switch
            {
                ParameterSpecType.Text => SpecTypeId.String.Text,
                ParameterSpecType.MultilineText => SpecTypeId.String.MultilineText,
                ParameterSpecType.Url => SpecTypeId.String.Url,
                ParameterSpecType.YesNo => SpecTypeId.Boolean.YesNo,
                ParameterSpecType.Integer => SpecTypeId.Int.Integer,
                ParameterSpecType.Material => SpecTypeId.Reference.Material,

                ParameterSpecType.Number => SpecTypeId.Number,
                ParameterSpecType.Length => SpecTypeId.Length,
                ParameterSpecType.Area => SpecTypeId.Area,
                ParameterSpecType.Volume => SpecTypeId.Volume,
                ParameterSpecType.Angle => SpecTypeId.Angle,
                ParameterSpecType.Slope => SpecTypeId.Slope,
                ParameterSpecType.Currency => SpecTypeId.Currency,

                ParameterSpecType.Force => SpecTypeId.Force,
                ParameterSpecType.Moment => SpecTypeId.Moment,
                ParameterSpecType.Stress => SpecTypeId.Stress,
                ParameterSpecType.Mass => SpecTypeId.Mass,
                ParameterSpecType.MassPerUnitLength => SpecTypeId.MassPerUnitLength,
                ParameterSpecType.MassPerUnitArea => SpecTypeId.MassPerUnitArea,
                ParameterSpecType.ReinforcementArea => SpecTypeId.ReinforcementArea,
                ParameterSpecType.ReinforcementCover => SpecTypeId.ReinforcementCover,
                ParameterSpecType.ReinforcementSpacing => SpecTypeId.ReinforcementSpacing,

                ParameterSpecType.Flow => SpecTypeId.Flow,
                ParameterSpecType.PipeSize => SpecTypeId.PipeSize,
                ParameterSpecType.PipingPressure => SpecTypeId.PipingPressure,
                ParameterSpecType.PipingVelocity => SpecTypeId.PipingVelocity,

                _ => throw new ArgumentOutOfRangeException(
                    nameof(specType), specType, "지원하지 않는 SpecType")
            };
        public static ForgeTypeId ToGroupTypeId(ParameterGroupType groupType)
            => groupType switch
            {
                ParameterGroupType.IdentityData => GroupTypeId.IdentityData,
                ParameterGroupType.General => GroupTypeId.General,
                ParameterGroupType.Data => GroupTypeId.Data,
                ParameterGroupType.Constraints => GroupTypeId.Constraints,
                ParameterGroupType.Phasing => GroupTypeId.Phasing,
                ParameterGroupType.Graphics => GroupTypeId.Graphics,
                ParameterGroupType.Materials => GroupTypeId.Materials,
                ParameterGroupType.Geometry => GroupTypeId.Geometry,
                ParameterGroupType.Length => GroupTypeId.Length,
                ParameterGroupType.Structural => GroupTypeId.Structural,
                ParameterGroupType.StructuralAnalysis => GroupTypeId.StructuralAnalysis,
                ParameterGroupType.Mechanical => GroupTypeId.Mechanical,
                ParameterGroupType.MechanicalAirflow => GroupTypeId.MechanicalAirflow,
                ParameterGroupType.MechanicalLoads => GroupTypeId.MechanicalLoads,
                ParameterGroupType.Electrical => GroupTypeId.Electrical,
                ParameterGroupType.ElectricalLighting => GroupTypeId.ElectricalLighting,
                ParameterGroupType.ElectricalLoads => GroupTypeId.ElectricalLoads,
                ParameterGroupType.Plumbing => GroupTypeId.Plumbing,
                ParameterGroupType.EnergyAnalysis => GroupTypeId.EnergyAnalysis,
                ParameterGroupType.Construction => GroupTypeId.Construction,
                ParameterGroupType.FireProtection => GroupTypeId.FireProtection,
                ParameterGroupType.Ifc => GroupTypeId.Ifc,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(groupType), groupType, "지원하지 않는 GroupType")
            };
        public static BuiltInCategory ToBuiltInCategory(ParameterCategory category)
            => category switch
            {
                ParameterCategory.GenericModel => BuiltInCategory.OST_GenericModel,
                ParameterCategory.Walls => BuiltInCategory.OST_Walls,
                ParameterCategory.Floors => BuiltInCategory.OST_Floors,
                ParameterCategory.StructuralColumns => BuiltInCategory.OST_StructuralColumns,
                ParameterCategory.StructuralFraming => BuiltInCategory.OST_StructuralFraming,
                ParameterCategory.StructuralFoundation => BuiltInCategory.OST_StructuralFoundation,
                ParameterCategory.ProjectInformation => BuiltInCategory.OST_ProjectInformation,
                ParameterCategory.Stairs => BuiltInCategory.OST_Stairs,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(category), category, "지원하지 않는 Category")
            };
        public static CategorySet ToCategorySet(IReadOnlyList<ParameterCategory> categories, Document document)
        {
            var categorySet = new CategorySet();
            foreach (var cat in categories)
            {
                var revitCategory = document.Settings.Categories.get_Item(ToBuiltInCategory(cat));
                categorySet.Insert(revitCategory);
            }
            return categorySet;
        }
        public static Binding ToBinding(ParameterBindingType bindingType, CategorySet categorySet, App application)
            => bindingType switch
            {
                ParameterBindingType.Instance => application.Create.NewInstanceBinding(categorySet),
                ParameterBindingType.Type => application.Create.NewTypeBinding(categorySet),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(bindingType), bindingType, "지원하지 않는 BindingType")
            };
    }
}