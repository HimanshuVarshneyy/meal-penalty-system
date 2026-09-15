using MealPenalty.Domain;

namespace MealPenalty.Application.Abstractions;

public interface IRuleSetRepository
{
    Task<IReadOnlyList<PenaltyRuleSet>> ListSetsAsync();
    Task<PenaltyRuleSet?> GetByIdAsync(int id);
    Task<PenaltyRuleSet?> GetActiveAsync();
    Task<int> InsertRuleSetAsync(PenaltyRuleSet ruleSet);
    Task ReplaceRulesAsync(int ruleSetId, IReadOnlyList<PenaltyRule> rules);
    Task ActivateAsync(int ruleSetId);
}
