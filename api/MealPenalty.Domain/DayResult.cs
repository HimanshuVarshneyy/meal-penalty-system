namespace MealPenalty.Domain;

public class DayResult
{
    public string Label { get; set; } = string.Empty;
    public DateOnly WorkDate { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal PaidHours { get; set; }
    public decimal TotalPenalty { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public List<Window> Windows { get; set; } = new();
    public List<PenaltyTriggerResult> Triggers { get; set; } = new();
}
