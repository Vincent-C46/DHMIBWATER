using DHBIMWATER.Core.Quantity.RuleSets;

namespace DHBIMWATER.Core.Settings
{
    public class ProjectSettings
    {
        public string Version { get; set; } = "1.0";
        public string Name { get; set; } = string.Empty;
        public RuleSet? RuleSet { get; set; }
        public FormworkSettings Formwork { get; set; } = new();
        public RebarSettings Rebar { get; set; } = new();
        public QuantitySettings Quantity { get; set; } = new();
    }
}
