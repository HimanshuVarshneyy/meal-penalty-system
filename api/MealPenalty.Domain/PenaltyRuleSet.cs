namespace MealPenalty.Domain;

public class PenaltyRuleSet
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;

    public List<PenaltyRule> Rules { get; set; } = new();
}
