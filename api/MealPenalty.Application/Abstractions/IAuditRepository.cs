using MealPenalty.Domain;

namespace MealPenalty.Application.Abstractions;

public interface IAuditRepository
{
    Task InsertAsync(PenaltyRuleAudit audit);
    Task<IReadOnlyList<PenaltyRuleAudit>> ListAsync(int? ruleSetId, DateOnly? from, DateOnly? to);
}
