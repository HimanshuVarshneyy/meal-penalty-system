namespace MealPenalty.Domain;

public class PenaltyRule
{
    public int Id { get; set; }
    public int RuleSetId { get; set; }
    public int RowOrder { get; set; }
    public decimal StartHours { get; set; }
    public decimal EndHours { get; set; }
    public PenaltyType PenaltyType { get; set; }
    public decimal Value { get; set; }
    public decimal? IntervalHours { get; set; }
}
