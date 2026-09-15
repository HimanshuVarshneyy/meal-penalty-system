using MealPenalty.Domain;

namespace MealPenalty.Application.Windows;

/// <summary>
/// Splits a day into windows at each meal break: In-&gt;Meal1Start, Meal1End-&gt;Meal2Start, ...,
/// LastMealEnd-&gt;Out. Each meal resets the penalty timer, so every window is evaluated on its own.
/// </summary>
public class DefaultWindowSplitter : IWindowSplitter
{
    public IReadOnlyList<Window> Split(TimesheetEntry entry)
    {
        var windows = new List<Window>();
        var meals = entry.MealBreaks.OrderBy(m => m.SeqNo).ToList();

        var cursor = entry.InHours;
        var index = 0;
        foreach (var meal in meals)
        {
            windows.Add(new Window { Index = index++, Start = cursor, End = meal.MealStart });
            cursor = meal.MealEnd;
        }

        windows.Add(new Window { Index = index, Start = cursor, End = entry.OutHours });
        return windows;
    }
}
