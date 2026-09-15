namespace MealPenalty.Domain;

public class TimesheetEntry
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public DateOnly WorkDate { get; set; }
    public decimal InHours { get; set; }
    public decimal OutHours { get; set; }
    public decimal HourlyRate { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<MealBreak> MealBreaks { get; set; } = new();
}
