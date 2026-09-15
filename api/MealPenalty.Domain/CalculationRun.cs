namespace MealPenalty.Domain;

public class CalculationRun
{
    public int Id { get; set; }
    public int TimesheetEntryId { get; set; }
    public int RuleSetId { get; set; }
    public string Label { get; set; } = string.Empty;
    public DateOnly WorkDate { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal PaidHours { get; set; }
    public decimal TotalPenalty { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;

    public List<CalculationRunDetail> Details { get; set; } = new();
}

public class CalculationRunDetail
{
    public int Id { get; set; }
    public int CalculationRunId { get; set; }
    public int WindowIndex { get; set; }
    public decimal WindowStart { get; set; }
    public decimal WindowEnd { get; set; }
    public int? RuleId { get; set; }
    public int TriggerCount { get; set; }
    public decimal PenaltyAmount { get; set; }
    public string Description { get; set; } = string.Empty;
}
