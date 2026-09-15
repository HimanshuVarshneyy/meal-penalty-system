using MealPenalty.Domain;

namespace MealPenalty.Application.Abstractions;

public interface ITimesheetRepository
{
    Task<int> InsertEntryAsync(TimesheetEntry entry);
}
