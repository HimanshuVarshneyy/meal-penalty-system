using MealPenalty.Domain;

namespace MealPenalty.Api.Dtos;

public class MealBreakDto
{
    public decimal MealStart { get; set; }
    public decimal MealEnd { get; set; }
}

public class TimesheetEntryDto
{
    public string Label { get; set; } = string.Empty;
    public DateOnly WorkDate { get; set; }
    public decimal InHours { get; set; }
    public decimal OutHours { get; set; }
    public decimal HourlyRate { get; set; }
    public List<MealBreakDto> MealBreaks { get; set; } = [];

    public TimesheetEntry ToDomain() => new()
    {
        Label = Label,
        WorkDate = WorkDate,
        InHours = InHours,
        OutHours = OutHours,
        HourlyRate = HourlyRate,
        MealBreaks = MealBreaks
            .OrderBy(m => m.MealStart)
            .Select((m, i) => new MealBreak { SeqNo = i + 1, MealStart = m.MealStart, MealEnd = m.MealEnd })
            .ToList()
    };
}

public class SimulateRequest
{
    public int RuleSetId { get; set; }
    public List<TimesheetEntryDto> Entries { get; set; } = [];
}
