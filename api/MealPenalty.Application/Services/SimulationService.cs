using MealPenalty.Application.Abstractions;
using MealPenalty.Application.Engine;
using MealPenalty.Domain;

namespace MealPenalty.Application.Services;

public class SimulationService
{
    private readonly IPenaltyCalculationEngine _engine;
    private readonly IRuleSetRepository _ruleSets;
    private readonly ITimesheetRepository _timesheets;
    private readonly ICalculationRunRepository _runs;

    public SimulationService(
        IPenaltyCalculationEngine engine,
        IRuleSetRepository ruleSets,
        ITimesheetRepository timesheets,
        ICalculationRunRepository runs)
    {
        _engine = engine;
        _ruleSets = ruleSets;
        _timesheets = timesheets;
        _runs = runs;
    }

    public async Task<IReadOnlyList<DayResult>> SimulateAsync(IReadOnlyList<TimesheetEntry> entries, int ruleSetId)
    {
        var ruleSet = await _ruleSets.GetByIdAsync(ruleSetId)
            ?? throw new KeyNotFoundException($"Rule set {ruleSetId} not found.");

        return entries.Select(entry => _engine.Calculate(entry, ruleSet)).ToList();
    }

    public async Task<IReadOnlyList<(TimesheetEntry Entry, DayResult Result, int RunId)>> SimulateAndSaveAsync(
        IReadOnlyList<TimesheetEntry> entries, int ruleSetId, string actor)
    {
        var ruleSet = await _ruleSets.GetByIdAsync(ruleSetId)
            ?? throw new KeyNotFoundException($"Rule set {ruleSetId} not found.");

        var saved = new List<(TimesheetEntry, DayResult, int)>();
        foreach (var entry in entries)
        {
            var result = _engine.Calculate(entry, ruleSet);
            var timesheetId = await _timesheets.InsertEntryAsync(entry);

            var run = new CalculationRun
            {
                TimesheetEntryId = timesheetId,
                RuleSetId = ruleSetId,
                Label = result.Label,
                WorkDate = result.WorkDate,
                HourlyRate = result.HourlyRate,
                PaidHours = result.PaidHours,
                TotalPenalty = result.TotalPenalty,
                TotalPaidAmount = result.TotalPaidAmount,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = actor,
                Details = result.Triggers.Select(t => new CalculationRunDetail
                {
                    WindowIndex = t.WindowIndex,
                    WindowStart = t.WindowStart,
                    WindowEnd = t.WindowEnd,
                    RuleId = t.RuleId,
                    TriggerCount = t.TriggerCount,
                    PenaltyAmount = t.PenaltyAmount,
                    Description = t.Description
                }).ToList()
            };

            var runId = await _runs.InsertRunAsync(run);
            saved.Add((entry, result, runId));
        }

        return saved;
    }
}
