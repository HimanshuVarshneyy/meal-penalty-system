using MealPenalty.Domain;

namespace MealPenalty.Application.Windows;

public interface IWindowSplitter
{
    IReadOnlyList<Window> Split(TimesheetEntry entry);
}
