using System.Text.Json;
using System.Text.Json.Serialization;
using MealPenalty.Application.Abstractions;
using MealPenalty.Domain;

namespace MealPenalty.Application.Services;

/// <summary>
/// Owns every write to a rule set and guarantees each one produces exactly one audit row in the
/// same operation. A rule set that is currently active is immutable - to change live rules you
/// create a new version and activate it, so historical calculations stay reproducible.
/// </summary>
public class RuleSetService
{
    // camelCase + string enums so the audit JSON matches the API's own RuleRow shape and the
    // frontend can parse it directly into a diff, instead of showing raw PascalCase/int JSON.
    private static readonly JsonSerializerOptions AuditJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IRuleSetRepository _ruleSets;
    private readonly IAuditRepository _audit;

    public RuleSetService(IRuleSetRepository ruleSets, IAuditRepository audit)
    {
        _ruleSets = ruleSets;
        _audit = audit;
    }

    public Task<IReadOnlyList<PenaltyRuleSet>> ListAsync() => _ruleSets.ListSetsAsync();

    public Task<PenaltyRuleSet?> GetAsync(int id) => _ruleSets.GetByIdAsync(id);

    public Task<PenaltyRuleSet?> GetActiveAsync() => _ruleSets.GetActiveAsync();

    public async Task<int> CreateAsync(string name, IReadOnlyList<PenaltyRule> rules, string actor, string? reason)
    {
        var ruleSet = new PenaltyRuleSet
        {
            Name = name,
            IsActive = false,
            EffectiveFrom = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = actor,
            Rules = rules.ToList()
        };

        var id = await _ruleSets.InsertRuleSetAsync(ruleSet);

        await _audit.InsertAsync(new PenaltyRuleAudit
        {
            RuleSetId = id,
            Action = AuditAction.Create,
            ChangedBy = actor,
            ChangedAt = DateTime.UtcNow,
            NewValueJson = JsonSerializer.Serialize(rules, AuditJsonOptions),
            Reason = reason
        });

        return id;
    }

    public async Task ReplaceRulesAsync(int ruleSetId, IReadOnlyList<PenaltyRule> rules, string actor, string? reason)
    {
        var existing = await _ruleSets.GetByIdAsync(ruleSetId)
            ?? throw new KeyNotFoundException($"Rule set {ruleSetId} not found.");

        if (existing.IsActive)
        {
            throw new InvalidOperationException(
                "Cannot edit rows of an active rule set. Create a new version instead.");
        }

        var oldJson = JsonSerializer.Serialize(existing.Rules, AuditJsonOptions);
        await _ruleSets.ReplaceRulesAsync(ruleSetId, rules);

        await _audit.InsertAsync(new PenaltyRuleAudit
        {
            RuleSetId = ruleSetId,
            Action = AuditAction.Update,
            ChangedBy = actor,
            ChangedAt = DateTime.UtcNow,
            OldValueJson = oldJson,
            NewValueJson = JsonSerializer.Serialize(rules, AuditJsonOptions),
            Reason = reason
        });
    }

    public async Task ActivateAsync(int ruleSetId, string actor, string? reason)
    {
        _ = await _ruleSets.GetByIdAsync(ruleSetId)
            ?? throw new KeyNotFoundException($"Rule set {ruleSetId} not found.");

        await _ruleSets.ActivateAsync(ruleSetId);

        await _audit.InsertAsync(new PenaltyRuleAudit
        {
            RuleSetId = ruleSetId,
            Action = AuditAction.Activate,
            ChangedBy = actor,
            ChangedAt = DateTime.UtcNow,
            Reason = reason
        });
    }
}
