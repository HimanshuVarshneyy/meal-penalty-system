using MealPenalty.Application.PenaltyTypes;
using MealPenalty.Application.Windows;
using MealPenalty.Domain;

namespace MealPenalty.Application.Engine;

/// <summary>
/// Orchestrates window splitting and per-window rule evaluation. New rule *types* plug in via
/// IPenaltyTypeHandler; new timesheet-shape logic plugs in via IWindowSplitter. This class only
/// coordinates the two - it never needs to change to support a new PenaltyType or splitting rule.
/// </summary>
public class PenaltyCalculationEngine : IPenaltyCalculationEngine
{
    private readonly IWindowSplitter _windowSplitter;
    private readonly IReadOnlyDictionary<PenaltyType, IPenaltyTypeHandler> _handlers;

    public PenaltyCalculationEngine(IWindowSplitter windowSplitter, IEnumerable<IPenaltyTypeHandler> handlers)
    {
        _windowSplitter = windowSplitter;
        _handlers = handlers.ToDictionary(h => h.PenaltyType);
    }

    public DayResult Calculate(TimesheetEntry entry, PenaltyRuleSet ruleSet)
    {
        var windows = _windowSplitter.Split(entry);
        var mealHours = entry.MealBreaks.Sum(m => m.MealEnd - m.MealStart);
        var paidHours = entry.OutHours - entry.InHours - mealHours;

        var triggers = new List<PenaltyTriggerResult>();
        foreach (var window in windows)
        {
            foreach (var rule in ruleSet.Rules.OrderBy(r => r.RowOrder))
            {
                var trigger = EvaluateRow(window, rule, entry.HourlyRate);
                if (trigger is not null)
                {
                    triggers.Add(trigger);
                }
            }
        }

        var totalPenalty = triggers.Sum(t => t.PenaltyAmount);
        var wages = paidHours * entry.HourlyRate;

        return new DayResult
        {
            Label = entry.Label,
            WorkDate = entry.WorkDate,
            HourlyRate = entry.HourlyRate,
            PaidHours = paidHours,
            TotalPenalty = totalPenalty,
            TotalPaidAmount = wages + totalPenalty,
            Windows = windows.ToList(),
            Triggers = triggers
        };
    }

    private PenaltyTriggerResult? EvaluateRow(Window window, PenaltyRule rule, decimal hourlyRate)
    {
        // Window must be strictly longer than Start to trigger.
        if (window.Length <= rule.StartHours)
        {
            return null;
        }

        // End caps the row: only the portion of the window between Start and End counts here.
        var countedHours = Math.Min(window.Length, rule.EndHours) - rule.StartHours;
        if (countedHours <= 0)
        {
            return null;
        }

        var triggerCount = rule.IntervalHours is { } interval and > 0
            ? (int)Math.Ceiling(countedHours / interval)
            : 1;

        if (triggerCount <= 0)
        {
            return null;
        }

        if (!_handlers.TryGetValue(rule.PenaltyType, out var handler))
        {
            throw new InvalidOperationException($"No handler registered for penalty type '{rule.PenaltyType}'.");
        }

        var perTrigger = handler.PriceSingleTrigger(rule, hourlyRate);
        var total = perTrigger * triggerCount;

        return new PenaltyTriggerResult
        {
            WindowIndex = window.Index,
            WindowStart = window.Start,
            WindowEnd = window.End,
            RuleId = rule.Id,
            TriggerCount = triggerCount,
            PenaltyAmount = total,
            Description = handler.Describe(rule, triggerCount, perTrigger, total)
        };
    }
}
