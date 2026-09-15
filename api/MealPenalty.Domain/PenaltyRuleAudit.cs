namespace MealPenalty.Domain;

public class PenaltyRuleAudit
{
    public int Id { get; set; }
    public int RuleSetId { get; set; }
    public int? RuleId { get; set; }
    public AuditAction Action { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string? OldValueJson { get; set; }
    public string? NewValueJson { get; set; }
    public string? Reason { get; set; }
}
