using DHBIMWATER.Core.Quantity;

namespace DHBIMWATER.Application.Interfaces.Quantity
{
    public interface IQuantityRuleRepository
    {
        RuleSet? GetProjectRuleSet();
        void SaveProjectRuleSet(RuleSet ruleSet);
    }
}
