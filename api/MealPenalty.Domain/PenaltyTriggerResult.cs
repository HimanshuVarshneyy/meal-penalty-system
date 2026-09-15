namespace MealPenalty.Domain;

public class PenaltyTriggerResult
{
    public int WindowIndex { get; set; }
    public decimal WindowStart { get; set; }
    public decimal WindowEnd { get; set; }
    public int? RuleId { get; set; }
    public int TriggerCount { get; set; }
    public decimal PenaltyAmount { get; set; }
    public string Description { get; set; } = string.Empty;
}
